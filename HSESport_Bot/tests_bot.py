import pytest
import asyncio
from unittest.mock import MagicMock, patch, AsyncMock
import pyodbc
import telegram

import HseSportBot
from HseSportBot import get_db_connection, get_student_attendance, start, handle_email, handle_choice, cancel, EMAIL, CHOICE, \
    ConversationHandler, connection_pool


@pytest.fixture
def mock_db_connection():
    mock_conn = MagicMock()
    mock_cursor = MagicMock()
    mock_conn.cursor.return_value = mock_cursor

    mock_get_db_connection_context_manager = MagicMock()
    mock_get_db_connection_context_manager.__enter__.return_value = mock_conn
    mock_get_db_connection_context_manager.__exit__.return_value = False

    with patch('HseSportBot.pyodbc.connect', return_value=mock_conn) as mock_pyodbc_connect:
        with patch('HseSportBot.get_db_connection', return_value=mock_get_db_connection_context_manager) as mock_get_db:
            yield mock_conn, mock_cursor


@pytest.fixture
def mock_update_context():
    mock_update = AsyncMock()
    mock_update.message.reply_text = AsyncMock()
    mock_context = MagicMock()
    mock_context.user_data = {}
    yield mock_update, mock_context


# --- Tests for database functions ---

@pytest.mark.asyncio
async def test_get_db_connection_success(mock_db_connection):
    # Arrange
    mock_conn, _ = mock_db_connection
    with patch('HseSportBot.connection_pool', []):
        # Act
        with get_db_connection() as conn:
            # Assert
            assert conn is not None
            HseSportBot.pyodbc.connect.assert_called_once()
        assert len(HseSportBot.connection_pool) == 1
        assert HseSportBot.connection_pool[0] == mock_conn


@pytest.mark.asyncio
async def test_get_db_connection_reuse_from_pool(mock_db_connection):
    # Arrange
    mock_conn, _ = mock_db_connection
    initial_conn = MagicMock()
    with patch('HseSportBot.connection_pool', [initial_conn]):
        # Act
        with get_db_connection() as conn:
            # Assert
            assert conn is not None
            assert conn == initial_conn
            HseSportBot.pyodbc.connect.assert_not_called()
        assert len(HseSportBot.connection_pool) == 1
        assert HseSportBot.connection_pool[0] == initial_conn


@pytest.mark.asyncio
async def test_get_db_connection_failure(mocker):
    # Arrange
    mocker.patch('HseSportBot.pyodbc.connect', side_effect=pyodbc.Error("Test DB Connection Error"))
    # Act
    with get_db_connection() as conn:
        # Assert
        assert conn is None
    assert len(HseSportBot.connection_pool) == 0


@pytest.mark.asyncio
async def test_get_student_attendance_success(mock_db_connection):
    # Arrange
    mock_conn, mock_cursor = mock_db_connection
    mock_cursor.fetchone.return_value = (1, "Иван", "Иванов", 25)
    mock_cursor.fetchall.return_value = [
        ("Футбол", 10),
        ("Баскетбол", 15)
    ]
    # Act
    name, surname, total_attendance, section_attendances, error = await get_student_attendance("ivan.ivanov@hse.ru")
    # Assert
    assert name == "Иван"
    assert surname == "Иванов"
    assert total_attendance == 25
    assert section_attendances == [{'name': 'Футбол', 'count': 10}, {'name': 'Баскетбол', 'count': 15}]
    assert error is None
    mock_cursor.execute.assert_any_call("SELECT StudentId, Name, Surname, AttendanceRate FROM Students WHERE Email = ?",
                                        "ivan.ivanov@hse.ru")
    mock_cursor.execute.assert_any_call(
        """
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
                    """, 1)


@pytest.mark.asyncio
async def test_get_student_attendance_student_not_found(mock_db_connection):
    # Arrange
    mock_conn, mock_cursor = mock_db_connection
    mock_cursor.fetchone.return_value = None
    # Act
    name, surname, total_attendance, section_attendances, error = await get_student_attendance("no.student@hse.ru")
    # Assert
    assert name is None
    assert surname is None
    assert total_attendance is None
    assert section_attendances is None
    assert "Студент с такой почтой не найден" in error


@pytest.mark.asyncio
async def test_get_student_attendance_db_query_error(mock_db_connection):
    # Arrange
    mock_conn, mock_cursor = mock_db_connection
    mock_cursor.execute.side_effect = pyodbc.Error("SQL Query Failed")
    # Act
    name, surname, total_attendance, section_attendances, error = await get_student_attendance("test@hse.ru")
    # Assert
    assert name is None
    assert surname is None
    assert total_attendance is None
    assert section_attendances is None
    assert "Ошибка запроса к базе данных: SQL Query Failed" in error


@pytest.mark.asyncio
async def test_get_student_attendance_unexpected_error(mocker):
    # Arrange
    mocker.patch('HseSportBot.get_db_connection', side_effect=Exception("Unexpected Error"))
    # Act
    name, surname, total_attendance, section_attendances, error = await get_student_attendance("test@hse.ru")
    # Assert
    assert name is None
    assert surname is None
    assert total_attendance is None
    assert section_attendances is None
    assert "Неизвестная ошибка при получении данных: Unexpected Error" in error


# --- Tests for Telegram handlers ---

@pytest.mark.asyncio
async def test_start_command(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    # Act
    result = await start(mock_update, mock_context)
    # Assert
    mock_update.message.reply_text.assert_called_once_with(
        "Привет! Я бот для проверки посещений. Введи свою корпоративную почту."
    )
    assert result == EMAIL


@pytest.mark.asyncio
async def test_handle_email_success(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "success@hse.ru"
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = (
        "Тестовый", "Студент", 30, [{'name': 'Тестовая Секция', 'count': 20}], None)
        # Act
        result = await handle_email(mock_update, mock_context)
        # Assert
        mock_get_student_attendance.assert_called_once_with("success@hse.ru")
        mock_update.message.reply_text.assert_called_once()
        response_text = mock_update.message.reply_text.call_args[0][0]
        assert "Студент: Тестовый Студент" in response_text
        assert "Количество посещений общее = 30" in response_text
        assert "Тестовая Секция: 20" in response_text
        assert mock_context.user_data['email'] == "success@hse.ru"
        assert mock_context.user_data['name'] == "Тестовый"
        assert mock_context.user_data['total_attendance'] == 30
        assert mock_context.user_data['section_attendances'] == [{'name': 'Тестовая Секция', 'count': 20}]
        assert result == CHOICE


@pytest.mark.asyncio
async def test_handle_email_student_not_found_error(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "unknown@hse.ru"
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = (None, None, None, None, "Студент с такой почтой не найден")
        # Act
        result = await handle_email(mock_update, mock_context)
        # Assert
        mock_get_student_attendance.assert_called_once_with("unknown@hse.ru")
        mock_update.message.reply_text.assert_called_once_with(
            "Студент с такой почтой не найден\n\nВведи почту заново или напиши /cancel для завершения."
        )
        assert result == EMAIL


@pytest.mark.asyncio
async def test_handle_choice_view_again(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Посмотреть ещё раз"
    mock_context.user_data['email'] = "cached_student@hse.ru"
    with patch('HseSportBot.get_student_attendance', new_callable=AsyncMock) as mock_get_student_attendance:
        mock_get_student_attendance.return_value = ("Кэшированный", "Студент", 40, [{'name': 'Йога', 'count': 8}], None)
        # Act
        result = await handle_choice(mock_update, mock_context)
        # Assert
        mock_get_student_attendance.assert_called_once_with("cached_student@hse.ru")
        mock_update.message.reply_text.assert_called_once()
        response_text = mock_update.message.reply_text.call_args[0][0]
        assert "Студент: Кэшированный Студент" in response_text
        assert "Количество посещений общее = 40" in response_text
        assert "Йога: 8" in response_text
        assert result == CHOICE


@pytest.mark.asyncio
async def test_handle_choice_enter_other_email(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Ввести другую почту"
    # Act
    result = await handle_choice(mock_update, mock_context)
    # Assert
    mock_update.message.reply_text.assert_called_once_with(
        "Введи новую корпоративную почту.",
        reply_markup=telegram.ReplyKeyboardRemove()
    )
    assert result == EMAIL


@pytest.mark.asyncio
async def test_handle_choice_invalid_option(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    mock_update.message.text = "Какая-то ерунда"
    # Act
    result = await handle_choice(mock_update, mock_context)
    # Assert
    mock_update.message.reply_text.assert_called_once_with(
        "Пожалуйста, выбери одну из предложенных опций."
    )
    assert result == CHOICE


@pytest.mark.asyncio
async def test_cancel_command(mock_update_context):
    # Arrange
    mock_update, mock_context = mock_update_context
    # Act
    result = await cancel(mock_update, mock_context)
    # Assert
    mock_update.message.reply_text.assert_called_once_with(
        "Работа бота завершена. Чтобы начать заново, напиши /start.",
        reply_markup=telegram.ReplyKeyboardRemove()
    )
    assert result == ConversationHandler.END


# --- Tests for shutdown function ---

@pytest.mark.asyncio
async def test_shutdown_closes_connections():
    # Arrange
    mock_conn1 = MagicMock()
    mock_conn2 = MagicMock()
    with patch('HseSportBot.connection_pool', [mock_conn1, mock_conn2]):
        # Act
        await HseSportBot.shutdown()
        # Assert
        mock_conn1.close.assert_called_once()
        mock_conn2.close.assert_called_once()
        assert len(HseSportBot.connection_pool) == 0


@pytest.mark.asyncio
async def test_shutdown_handles_close_errors():
    # Arrange
    mock_conn1 = MagicMock()
    mock_conn2 = MagicMock()
    mock_conn1.close.side_effect = Exception("Failed to close conn1")
    with patch('HseSportBot.connection_pool', [mock_conn1, mock_conn2]):
        with patch('builtins.print') as mock_print:
            # Act
            await HseSportBot.shutdown()
            # Assert
            mock_conn1.close.assert_called_once()
            mock_conn2.close.assert_called_once()
            mock_print.assert_called_once()
            assert "Ошибка при закрытии соединения из пула: Failed to close conn1" in mock_print.call_args[0][0]
            assert len(HseSportBot.connection_pool) == 0