import telegram
from telegram.ext import Application, CommandHandler, ConversationHandler, MessageHandler, filters
import pyodbc
from contextlib import contextmanager
from cachetools import TTLCache
import asyncio
from uuid import uuid4

EMAIL, CHOICE = range(2)

# Настройка пула соединений
CONNECTION_STRING = (
    'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=Chazik\SQLEXPRESS;'
    r'DATABASE=HSESport;'
    'Trusted_Connection=yes;'
)

# Пул соединений (максимум 30 соединений)
connection_pool = []
MAX_CONNECTIONS = 30  # Увеличено для поддержки до 30 одновременных соединений
connection_semaphore = asyncio.Semaphore(MAX_CONNECTIONS)

# Кэш для данных о посещениях (храним 1 час)
cache = TTLCache(maxsize=1000, ttl=10)


@contextmanager
def get_db_connection():
    conn = None
    try:
        if connection_pool:
            conn = connection_pool.pop()
        else:
            conn = pyodbc.connect(CONNECTION_STRING)
        yield conn
    except pyodbc.Error as e:
        print(f"Ошибка подключения к базе данных: {e}")
        yield None
    finally:
        if conn:
            connection_pool.append(conn)


# В файле бота, в функции get_student_attendance
async def get_student_attendance(email):
    # Удаляем проверку кэша, как было сделано ранее
    # cache_key = f"attendance_{email}"
    # if cache_key in cache:
    #     return cache[cache_key], None

    async with connection_semaphore:
        with get_db_connection() as conn:
            if not conn:
                return None, None, "Ошибка подключения к базе данных"

            try:
                cursor = conn.cursor()
                # ИЗМЕНЕНИЕ: Запрашиваем Name, Surname и AttendanceRate
                query = "SELECT Name, Surname, AttendanceRate FROM Students WHERE Email = ?"
                cursor.execute(query, email)
                result = cursor.fetchone()

                if result:
                    # Возвращаем имя, фамилию и количество посещений
                    name = result[0]
                    surname = result[1]
                    attendance_rate = result[2]
                    # cache[cache_key] = (name, surname, attendance_rate) # Удаляем запись в кэш
                    return name, surname, attendance_rate, None
                else:
                    return None, None, None, "Студент с такой почтой не найден"
            except pyodbc.Error as e:
                return None, None, None, f"Ошибка запроса к базе данных: {e}"


async def start(update, context):
    await update.message.reply_text(
        "Привет! Я бот для проверки посещений. Введи свою корпоративную почту."
    )
    return EMAIL


async def handle_email(update, context):
    email = update.message.text
    context.user_data['email'] = email

    # ИЗМЕНЕНИЕ: Получаем имя, фамилию и количество посещений
    name, surname, attendance, error = await get_student_attendance(email)

    if error:
        await update.message.reply_text(
            error + "\n\nВведи почту заново или напиши /cancel для завершения."
        )
        return EMAIL

    context.user_data['name'] = name
    context.user_data['surname'] = surname
    context.user_data['attendance'] = attendance

    keyboard = [
        [telegram.KeyboardButton("Посмотреть ещё раз")],
        [telegram.KeyboardButton("Ввести другую почту")]
    ]
    reply_markup = telegram.ReplyKeyboardMarkup(keyboard, one_time_keyboard=True)

    # ИЗМЕНЕНИЕ: Форматируем имя и фамилию, убирая лишние пробелы
    full_name_parts = []
    if name:
        full_name_parts.append(str(name).strip())
    if surname:
        full_name_parts.append(str(surname).strip())

    display_name = " ".join(full_name_parts)

    await update.message.reply_text(
        f"Студент: {display_name}\nКоличество посещений: {attendance}\n\nЧто хочешь сделать?",
        reply_markup=reply_markup
    )
    return CHOICE


async def handle_choice(update, context):
    choice = update.message.text

    if choice == "Посмотреть ещё раз":
        # Заново получаем email из user_data
        email = context.user_data.get('email')
        if not email:
            # Если email почему-то потерялся (что маловероятно в данном потоке),
            # просим ввести заново
            await update.message.reply_text("Что-то пошло не так. Пожалуйста, введи свою почту заново.")
            return EMAIL

        # >>> ЭТО КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: СНОВА ЗАПРАШИВАЕМ ДАННЫЕ ИЗ БД <<<
        # ИЗМЕНЕНИЕ: Получаем имя, фамилию и количество посещений
        name, surname, attendance, error = await get_student_attendance(email)

        if error:
            # Обрабатываем возможные ошибки при повторном запросе
            await update.message.reply_text(
                error + "\n\nВведи почту заново или напиши /cancel для завершения."
            )
            return EMAIL

        # Обновляем данные в user_data
        context.user_data['name'] = name
        context.user_data['surname'] = surname
        context.user_data['attendance'] = attendance

        keyboard = [
            [telegram.KeyboardButton("Посмотреть ещё раз")],
            [telegram.KeyboardButton("Ввести другую почту")]
        ]
        reply_markup = telegram.ReplyKeyboardMarkup(keyboard, one_time_keyboard=True)
        # ИЗМЕНЕНИЕ: Форматируем имя и фамилию, убирая лишние пробелы
        full_name_parts = []
        if name:
            full_name_parts.append(str(name).strip())
        if surname:
            full_name_parts.append(str(surname).strip())

        display_name = " ".join(full_name_parts)

        await update.message.reply_text(
            f"Студент: {display_name}\n\nКоличество посещений: {attendance}\n\nЧто хочешь сделать?",
            reply_markup=reply_markup
        )
        return CHOICE
    elif choice == "Ввести другую почту":
        await update.message.reply_text(
            "Введи новую корпоративную почту.",
            reply_markup=telegram.ReplyKeyboardRemove()
        )
        return EMAIL
    else:
        await update.message.reply_text(
            "Пожалуйста, выбери одну из предложенных опций."
        )
        return CHOICE


async def cancel(update, context):
    await update.message.reply_text(
        "Работа бота завершена. Чтобы начать заново, напиши /start.",
        reply_markup=telegram.ReplyKeyboardRemove()
    )
    return ConversationHandler.END


async def shutdown():
    for conn in connection_pool:
        conn.close()
    connection_pool.clear()


def main():
    # Замените 'YOUR_BOT_TOKEN' на токен вашего бота
    application = Application.builder().token('8064109215:AAFVxyyABHcw8uT4gzW7c2bVT_68-R6EZ_8').build()

    conv_handler = ConversationHandler(
        entry_points=[CommandHandler('start', start)],
        states={
            EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, handle_email)],
            CHOICE: [MessageHandler(filters.TEXT & ~filters.COMMAND, handle_choice)],
        },
        fallbacks=[CommandHandler('cancel', cancel)],
    )

    application.add_handler(conv_handler)
    application.add_handler(CommandHandler('shutdown', shutdown))

    application.run_polling()


if __name__ == '__main__':
    main()
