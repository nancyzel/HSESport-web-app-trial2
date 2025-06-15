import pytest
import asyncio
from unittest.mock import MagicMock, patch, AsyncMock
import pyodbc
import telegram  # Импортируем telegram для ReplyKeyboardRemove

# Импортируем сам модуль HseSportBot
import HseSportBot  # <--- Ключевое изменение: импортируем HseSportBot как модуль

# Импортируем все необходимое из вашего файла бота HseSportBot.py
from HseSportBot import get_db_connection, get_student_attendance, start, handle_email, handle_choice, cancel, EMAIL, CHOICE, \
    ConversationHandler, connection_pool


# --- Фикстуры для мокирования ---

@pytest.fixture
def mock_db_connection():
    """
    Фикстура для мокирования объекта соединения с базой данных и его курсора.
    Использует patch для перехвата pyodbc.connect и get_db_connection,
    чтобы предотвратить реальные подключения к БД.
    """
    mock_conn = MagicMock()
    mock_cursor = MagicMock()
    mock_conn.cursor.return_value = mock_cursor

    # Мы мокаем get_db_connection, чтобы он возвращал наш mock_conn,
    # а также pyodbc.connect, чтобы он не создавал реальные соединения.
    # ИЗМЕНЕНО: 'HseSportBot.pyodbc.connect'
    with patch('HseSportBot.pyodbc.connect', return_value=mock_conn) as mock_pyodbc_connect:
        # ИЗМЕНЕНО: 'HseSportBot.get_db_connection'
        with patch('HseSportBot.get_db_connection', return_value=mock_conn):  # Мокаем контекстный менеджер
            yield mock_conn, mock_cursor


@pytest.fixture
def mock_update_context():
    """
    Фикстура для имитации объектов telegram.Update и CallbackContext.
    Мокает метод reply_text для проверки отправляемых сообщений.
    """
    mock_update = AsyncMock()
    mock_update.message.reply_text = AsyncMock()  # Мокаем метод, который будет вызываться
    mock_context = MagicMock()
    mock_context.user_data = {}  # Имитируем user_data для хранения состояний
    yield mock_update, mock_context


# --- Тесты для функций работы с базой данных ---

@pytest.mark.asyncio
async def test_get_db_connection_success(mock_db_connection):
    """Тестирует успешное получение соединения из пула или создание нового."""
    mock_conn, _ = mock_db_connection

    # ИЗМЕНЕНО: 'HseSportBot.connection_pool'
    with patch('HseSportBot.connection_pool', []):
        with get_db_connection() as conn:
            assert conn is not None
            # ИЗМЕНЕНО: HseSportBot.pyodbc.connect
            HseSportBot.pyodbc.connect.assert_called_once()

        # ИЗМЕНЕНО: HseSportBot.connection_pool
        assert len(HseSportBot.connection_pool) == 1
        assert HseSportBot.connection_pool[0] == mock_conn


@pytest.mark.asyncio
async def test_get_db_connection_reuse_from_pool(mock_db_connection):
    """Тестирует повторное использование соединения из пула."""
    mock_conn, _ = mock_db_connection

    # Имитируем, что в пуле уже есть соединение
    initial_conn = MagicMock()
    # ИЗМЕНЕНО: 'HseSportBot.connection_pool'
    with patch('HseSportBot.connection_pool', [initial_conn]):
        with get_db_connection() as conn:
            assert conn is not None
            assert conn == initial_conn  # Должны получить соединение из пула
            # ИЗМЕНЕНО: HseSportBot.pyodbc.connect
            HseSportBot.pyodbc.connect.assert_not_called()  # pyodbc.connect не должен вызываться

        # ИЗМЕНЕНО: HseSportBot.connection_pool
        assert len(HseSportBot.connection_pool) == 1
        assert HseSportBot.connection_pool[0] == initial_conn


@pytest.mark.asyncio
async def test_get_db_connection_failure(mocker):
    """Тестирует ошибку при попытке подключения к базе данных."""
    # ИЗМЕНЕНО: 'HseSportBot.pyodbc.connect'
    mocker.patch('HseSportBot.pyodbc.connect', side_effect=pyodbc.Error("Test DB Connection Error"))

    with get_db_connection() as conn:
        assert conn is None
    # ИЗМЕНЕНО: HseSportBot.connection_pool
    assert len(HseSportBot.connection_pool) == 0

@pytest.mark.asyncio
async def test_get_student_attendance_unexpected_error(mocker):
    """Тестирует неожиданную ошибку внутри get_student_attendance."""
    # ИЗМЕНЕНО: 'HseSportBot.get_db_connection'
    mocker.patch('HseSportBot.get_db_connection', side_effect=Exception("Unexpected Error"))

    name, surname, total_attendance, section_attendances, error = await get_student_attendance("test@hse.ru")

    assert name is None
    assert surname is None
    assert total_attendance is None
    assert section_attendances is None
    assert "Неизвестная ошибка при получении данных: Unexpected Error" in error


# --- Тесты для обработчиков Telegram ---

@pytest.mark.asyncio
async def test_start_command(mock_update_context):
    """Тестирует команду /start."""
    mock_update, mock_context = mock_update_context
    result = await start(mock_update, mock_context)

    mock_update.message.reply_text.assert_called_once_with(
        "Привет! Я бот для проверки посещений. Введи свою корпоративную почту."
    )
    assert result == EMAIL


@pytest.mark.asyncio
async def test_handle_email_success(mock_update_context):
    """Тестирует успешную обработку почты и вывод данных."""
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "success@hse.ru"

    # ИЗМЕНЕНО: 'HseSportBot.get_student_attendance'
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = (
        "Тестовый", "Студент", 30, [{'name': 'Тестовая Секция', 'count': 20}], None)

        result = await handle_email(mock_update, mock_context)

        mock_get_student_attendance.assert_called_once_with("success@hse.ru")
        mock_update.message.reply_text.assert_called_once()

        # Проверяем содержимое отправленного сообщения
        response_text = mock_update.message.reply_text.call_args[0][0]
        assert "Студент: Тестовый Студент" in response_text
        assert "Количество посещений общее = 30" in response_text
        assert "Тестовая Секция: 20" in response_text

        # Проверяем, что user_data были правильно сохранены
        assert mock_context.user_data['email'] == "success@hse.ru"
        assert mock_context.user_data['name'] == "Тестовый"
        assert mock_context.user_data['total_attendance'] == 30
        assert mock_context.user_data['section_attendances'] == [{'name': 'Тестовая Секция', 'count': 20}]
        assert result == CHOICE


@pytest.mark.asyncio
async def test_handle_email_student_not_found_error(mock_update_context):
    """Тестирует обработку случая, когда студент не найден."""
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "unknown@hse.ru"

    # ИЗМЕНЕНО: 'HseSportBot.get_student_attendance'
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = (None, None, None, None, "Студент с такой почтой не найден")

        result = await handle_email(mock_update, mock_context)

        mock_get_student_attendance.assert_called_once_with("unknown@hse.ru")
        mock_update.message.reply_text.assert_called_once_with(
            "Студент с такой почтой не найден\n\nВведи почту заново или напиши /cancel для завершения."
        )
        assert result == EMAIL  # Должны вернуться в состояние EMAIL


@pytest.mark.asyncio
async def test_handle_choice_view_again(mock_update_context):
    """Тестирует опцию "Посмотреть ещё раз"."""
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Посмотреть ещё раз"
    # Имитируем, что email уже сохранен в user_data
    mock_context.user_data['email'] = "cached_student@hse.ru"

    # ИЗМЕНЕНО: 'HseSportBot.get_student_attendance'
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = ("Кэшированный", "Студент", 40, [{'name': 'Йога', 'count': 8}], None)

        result = await handle_choice(mock_update, mock_context)

        mock_get_student_attendance.assert_called_once_with("cached_student@hse.ru")
        mock_update.message.reply_text.assert_called_once()
        response_text = mock_update.message.reply_text.call_args[0][0]
        assert "Студент: Кэшированный Студент" in response_text
        assert "Количество посещений общее = 40" in response_text
        assert "Йога: 8" in response_text
        assert result == CHOICE


@pytest.mark.asyncio
async def test_handle_choice_enter_other_email(mock_update_context):
    """Тестирует опцию "Ввести другую почту"."""
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Ввести другую почту"

    result = await handle_choice(mock_update, mock_context)

    mock_update.message.reply_text.assert_called_once_with(
        "Введи новую корпоративную почту.",
        reply_markup=telegram.ReplyKeyboardRemove()
    )
    assert result == EMAIL


@pytest.mark.asyncio
async def test_handle_choice_invalid_option(mock_update_context):
    """Тестирует обработку невалидной опции выбора."""
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Какая-то ерунда"

    result = await handle_choice(mock_update, mock_context)

    mock_update.message.reply_text.assert_called_once_with(
        "Пожалуйста, выбери одну из предложенных опций."
    )
    assert result == CHOICE  # Остаемся в текущем состоянии


@pytest.mark.asyncio
async def test_cancel_command(mock_update_context):
    """Тестирует команду /cancel."""
    mock_update, mock_context = mock_update_context
    result = await cancel(mock_update, mock_context)

    mock_update.message.reply_text.assert_called_once_with(
        "Работа бота завершена. Чтобы начать заново, напиши /start.",
        reply_markup=telegram.ReplyKeyboardRemove()
    )
    assert result == ConversationHandler.END


# --- Тесты для функции shutdown ---

@pytest.mark.asyncio
async def test_shutdown_closes_connections():
    """
    Тестирует, что функция shutdown корректно закрывает все соединения
    в пуле и очищает его.
    """
    # Имитируем несколько открытых соединений в пуле
    mock_conn1 = MagicMock()
    mock_conn2 = MagicMock()

    # ИЗМЕНЕНО: 'HseSportBot.connection_pool'
    with patch('HseSportBot.connection_pool', [mock_conn1, mock_conn2]):
        # ИЗМЕНЕНО: HseSportBot.connection_pool
        assert len(HseSportBot.connection_pool) == 2  # Убеждаемся, что пул инициализирован

        # ИЗМЕНЕНО: await HseSportBot.shutdown()
        await HseSportBot.shutdown()

        # Проверяем, что каждое соединение было закрыто
        mock_conn1.close.assert_called_once()
        mock_conn2.close.assert_called_once()

        # Проверяем, что пул соединений был очищен
        assert len(HseSportBot.connection_pool) == 0


@pytest.mark.asyncio
async def test_shutdown_handles_close_errors():
    """Тестирует, что shutdown корректно обрабатывает ошибки при закрытии соединений."""
    mock_conn1 = MagicMock()
    mock_conn2 = MagicMock()
    mock_conn1.close.side_effect = Exception("Failed to close conn1")  # Имитируем ошибку закрытия

    # ИЗМЕНЕНО: 'HseSportBot.connection_pool'
    with patch('HseSportBot.connection_pool', [mock_conn1, mock_conn2]):
        with patch('builtins.print') as mock_print:
            # ИЗМЕНЕНО: await HseSportBot.shutdown()
            await HseSportBot.shutdown()

            mock_conn1.close.assert_called_once()
            mock_conn2.close.assert_called_once()  # Второе соединение должно закрыться успешно

            mock_print.assert_called_once()
            assert "Ошибка при закрытии соединения из пула: Failed to close conn1" in mock_print.call_args[0][0]
            # ИЗМЕНЕНО: HseSportBot.connection_pool
            assert len(HseSportBot.connection_pool) == 0