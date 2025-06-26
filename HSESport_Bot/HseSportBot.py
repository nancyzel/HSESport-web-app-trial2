import telegram
from telegram.ext import Application, CommandHandler, ConversationHandler, MessageHandler, filters, CallbackContext
import pyodbc
from contextlib import contextmanager
import asyncio


EMAIL, CHOICE = range(2)

# Настройка пула соединений
CONNECTION_STRING = (
    'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=Chazik\SQLEXPRESS;'
    r'DATABASE=TeachersDataBase;'
    'Trusted_Connection=yes;'
)

# Пул соединений (максимум 30 соединений)
connection_pool = []
MAX_CONNECTIONS = 30
connection_semaphore = asyncio.Semaphore(MAX_CONNECTIONS)


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


async def get_student_attendance(email):
    async with connection_semaphore:
        try:
            with get_db_connection() as conn:
                if not conn:
                    return None, None, None, None, "Ошибка подключения к базе данных"

                try:
                    cursor = conn.cursor()

                    # Запрос для получения основной информации о студенте
                    base_query = "SELECT StudentId, Name, Surname, AttendanceRate FROM Students WHERE Email = ?"
                    cursor.execute(base_query, email)
                    student_result = cursor.fetchone()

                    if not student_result:
                        return None, None, None, None, "Студент с такой почтой не найден"

                    student_id = student_result[0]
                    name = student_result[1]
                    surname = student_result[2]
                    total_attendance_rate = student_result[3]

                    # Запрос для получения посещаемости по секциям
                    section_attendance_query = """
                        SELECT
                            SEC.Name AS SectionName,
                            COUNT(AD.AttendanceId) AS SectionAttendanceCount
                        FROM
                            AttendanceDates AS AD
                        JOIN
                            Sections AS SEC ON AD.SectionId = SEC.SectionId
                        WHERE
                            AD.StudentId = ?
                        GROUP BY
                            SEC.Name
                        ORDER BY
                            SEC.Name;
                    """
                    cursor.execute(section_attendance_query, student_id)
                    section_results = cursor.fetchall()

                    section_attendances_list = []
                    for row in section_results:
                        section_attendances_list.append({'name': row[0], 'count': row[1]})

                    # Возвращаем имя, фамилию, общее количество посещений, список посещений по секциям и ошибку (None)
                    return name, surname, total_attendance_rate, section_attendances_list, None

                except pyodbc.Error as e:
                    print(f"Ошибка запроса к базе данных: {e}")
                    return None, None, None, None, f"Ошибка запроса к базе данных: {e}"
        except Exception as e:
            print(f"Неожиданная ошибка в get_student_attendance: {e}")
            return None, None, None, None, f"Неизвестная ошибка при получении данных: {e}"


async def start(update: telegram.Update, context: CallbackContext) -> int:

    await update.message.reply_text(
        "Привет! Я бот для проверки посещений. Введи свою корпоративную почту."
    )
    return EMAIL

async def _send_attendance_summary(update: telegram.Update, context: CallbackContext, email: str) -> int:

    name, surname, total_attendance, section_attendances, error = await get_student_attendance(email)

    if error:
        await update.message.reply_text(
            error + "\n\nВведи почту заново или напиши /cancel для завершения."
        )
        return EMAIL # Остаемся в состоянии EMAIL при ошибке

    # Сохраняем все данные в user_data для последующего использования
    context.user_data['name'] = name
    context.user_data['surname'] = surname
    context.user_data['total_attendance'] = total_attendance
    context.user_data['section_attendances'] = section_attendances
    context.user_data['email'] = email # Убедимся, что email сохранен

    # Создаем клавиатуру с опциями для пользователя
    keyboard = [
        [telegram.KeyboardButton("Посмотреть ещё раз")],
        [telegram.KeyboardButton("Ввести другую почту")]
    ]
    reply_markup = telegram.ReplyKeyboardMarkup(keyboard, one_time_keyboard=True)

    # Формируем отображаемое имя студента
    full_name_parts = []
    if name:
        full_name_parts.append(str(name).strip())
    if surname:
        full_name_parts.append(str(surname).strip())

    display_name = " ".join(full_name_parts)

    # Формируем текст ответа
    response_text = f"Студент: {display_name}\n\n"
    response_text += f"Общее количество посещений = {total_attendance}\n"

    if section_attendances:
        response_text += "\nПосещения по секциям:\n"
        for section in section_attendances:
            section_name_clean = str(section['name']).strip()
            section_count_clean = str(section['count']).strip()
            response_text += f"- {section_name_clean}: {section_count_clean}\n"
    else:
        response_text += "\nПосещения по секциям не найдены."

    # Отправляем сообщение со сводкой и клавиатурой
    await update.message.reply_text(
        response_text + "\n\nЧто хочешь сделать?",
        reply_markup=reply_markup
    )
    return CHOICE # Переходим в состояние CHOICE после успешного вывода


async def handle_email(update: telegram.Update, context: CallbackContext) -> int:
    email = update.message.text
    # Вызываем вспомогательную функцию для обработки email и отправки сводки
    return await _send_attendance_summary(update, context, email)


async def handle_choice(update: telegram.Update, context: CallbackContext) -> int:
    choice = update.message.text

    if choice == "Посмотреть ещё раз":
        email = context.user_data.get('email')
        if not email:
            # Если email почему-то потерян, просим ввести заново
            await update.message.reply_text("Что-то пошло не так. Пожалуйста, введи свою почту заново.")
            return EMAIL # Возвращаемся в состояние EMAIL

        # Вызываем вспомогательную функцию для повторной отправки сводки
        return await _send_attendance_summary(update, context, email)
    elif choice == "Ввести другую почту":
        await update.message.reply_text(
            "Введи новую корпоративную почту.",
            reply_markup=telegram.ReplyKeyboardRemove() # Убираем кнопки
        )
        return EMAIL # Переходим в состояние EMAIL
    else:
        # Если введен нераспознанный текст
        await update.message.reply_text(
            "Пожалуйста, выбери одну из предложенных опций."
        )
        return CHOICE # Остаемся в состоянии CHOICE


async def cancel(update: telegram.Update, context: CallbackContext) -> int:
    await update.message.reply_text(
        "Работа бота завершена. Чтобы начать заново, напиши /start.",
        reply_markup=telegram.ReplyKeyboardRemove() # Убираем кнопки
    )
    return ConversationHandler.END # Завершаем ConversationHandler


async def shutdown():
    for conn in connection_pool:
        try:
            conn.close()
        except Exception as e:
            print(f"Ошибка при закрытии соединения из пула: {e}")
    connection_pool.clear()

def main():
    # Создание объекта Application с токеном бота
    application = Application.builder().token('8064109215:AAFVxyyABHcw8uT4gzW7c2bVT_68-R6EZ_8').build()

    # Настройка ConversationHandler для управления состоянием диалога
    conv_handler = ConversationHandler(
        entry_points=[CommandHandler('start', start)], # Точка входа: команда /start
        states={
            EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, handle_email)], # Состояние EMAIL: ожидаем текст
            CHOICE: [MessageHandler(filters.TEXT & ~filters.COMMAND, handle_choice)], # Состояние CHOICE: ожидаем текст
        },
        fallbacks=[CommandHandler('cancel', cancel)], # Обработчик на случай отмены: команда /cancel
    )

    # Добавление обработчика диалога в приложение
    application.add_handler(conv_handler)
    # Добавление обработчика для команды /shutdown (для корректного завершения)
    application.add_handler(CommandHandler('shutdown', shutdown))

    # Запуск бота в режиме polling (опрос новых обновлений)
    application.run_polling()


if __name__ == '__main__':
    main()
