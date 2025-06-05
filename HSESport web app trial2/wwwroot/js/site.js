// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

<script>
    document.addEventListener('DOMContentLoaded', function () {
        const tabs = document.querySelectorAll('.tab-button');
    const forms = document.querySelectorAll('.tab-content');

    // Инициализация: показать активную форму
    showForm('teacher');

        // Обработчик кликов по табам
        tabs.forEach(tab => {
        tab.addEventListener('click', function (e) {
            e.preventDefault(); // Предотвращаем переход по ссылке

            // Удаляем класс "active" у всех табов
            tabs.forEach(t => t.classList.remove('active'));

            // Добавляем класс "active" к текущему табу
            this.classList.add('active');

            // Получаем значение data-tab из атрибута
            const targetTab = this.getAttribute('data-tab');

            // Показываем соответствующую форму
            showForm(targetTab);
        });
        });

    // Функция для показа/скрытия форм
    function showForm(tabName) {
        forms.forEach(form => {
            form.classList.add('hidden'); // Скрываем все формы
        });

    document.getElementById(`${tabName}-form`).classList.remove('hidden'); // Показываем нужную форму
        }
    });
</script>
