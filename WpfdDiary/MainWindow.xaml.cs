﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

using DayTasks;

using Hardcodet.Wpf.TaskbarNotification;

using ShortTaskWindow;

namespace WpfDiary
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ресурсы для текущей отображаемой информации
        private static class CalendarInfo
        {
            public static ShortAddTaskWindow ShortForm { get; set; }
            public static DateTime CurrentDate { get; set; }
            internal static TaskList TaskList { get; set; } = new TaskList();
            internal static Dictionary<TaskType, bool?> TypesState { get; set; } = new Dictionary<TaskType, bool?>();

            public static List<DayTask> SelectActiveTopics ()
            {
                var tasks = from dayTasks in TaskList.Tasks
                            where TypesState[dayTasks.Тип] == true
                            select dayTasks;
                return tasks.ToList<DayTask>();
            }
        }

        //инициальзация объектов и назначение обработчиков событий
        public MainWindow ()
        {
            InitializeComponent();
            taskbarIcon.PopupActivation = PopupActivationMode.LeftClick;
            CalendarInfo.CurrentDate = calendar.SelectedDate ?? calendar.DisplayDate;
            CalendarInfo.TaskList.LoadTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            tasksGrid.ItemsSource = CalendarInfo.TaskList.Tasks;

            //загрузка типов заданий
            taskTypesList.ItemsSource = Enum.GetValues(typeof(TaskType));
            taskTypesList.SelectedIndex = 0;

            calendar.SelectedDatesChanged += DatesChanged;

            foreach (ToggleButton button in ButtonGrid.Children)
            {
                button.Click += PressedTypeButton;
            }

            var index = 1;
            foreach (ToggleButton button in ButtonGrid.Children)
            {
                var myBrush = FindResource($"Br{index}") as SolidColorBrush;
                myBrush.Color = TaskTypeColors.colors[index - 1];
                index++;
            }

            foreach (TaskType type in Enum.GetValues(typeof(TaskType)))
            {
                CalendarInfo.TypesState[type] = true;
            }
        }

        //событие смены даты
        private void DatesChanged (object sender, RoutedEventArgs e)
        {
            CalendarInfo.TaskList.SaveTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            var date = (System.Windows.Controls.Calendar)sender;
            CalendarInfo.TaskList.LoadTaskList(TaskList.DateToJsonFileName((System.DateTime)date.SelectedDate));
            tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics();
            CalendarInfo.CurrentDate = (System.DateTime)date.SelectedDate;
        }

        //сохранение при выходе
        public void AppExit (object sender, CancelEventArgs e)
        {
            CalendarInfo.TaskList.SaveTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            Application.Current.Shutdown();
        }

        //изменение отображыемых видов задач
        public void PressedTypeButton (object sender, EventArgs e)
        {
            if (sender is ToggleButton button)
            {
                switch (button.Name)
                {
                    case "IdeasButton":
                        CalendarInfo.TypesState[TaskType.Идеи] = !button.IsChecked;
                        break;
                    case "WorkButton":
                        CalendarInfo.TypesState[TaskType.Работа] = !button.IsChecked;
                        break;
                    case "StudyButton":
                        CalendarInfo.TypesState[TaskType.Учёба] = !button.IsChecked;
                        break;
                    case "PurchasesButton":
                        CalendarInfo.TypesState[TaskType.Покупки] = !button.IsChecked;
                        break;
                    case "BirthdayButton":
                        CalendarInfo.TypesState[TaskType.Дни_Рождения] = !button.IsChecked;
                        break;
                    case "HouseholdChoresButton":
                        CalendarInfo.TypesState[TaskType.Домашние_Дела] = !button.IsChecked;
                        break;
                    case "ImportantMatterButton":
                        CalendarInfo.TypesState[TaskType.Важные_Дела] = !button.IsChecked;
                        break;
                }

                tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics() ?? new List<DayTask>();
            }
        }

        //добавить задачу
        private void AddTask (object sender, RoutedEventArgs e)
        {
            var newTask = new DayTask
            {
                Тип = (TaskType)taskTypesList.SelectedItem,
                Заголовок = nameTextBox.Text,
                Информация = infoTextBox.Text,
                Выполнено = false,
            };

            CalendarInfo.TaskList.Tasks.Add(newTask);
            tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics() ?? new List<DayTask>();
        }

        //удалить выделенные задачи
        private void DeleteTask (object sender, RoutedEventArgs e)
        {
            var selectedTasks = tasksGrid.SelectedItems;
            foreach (var elem in selectedTasks)
            {
                CalendarInfo.TaskList.Tasks.RemoveAll(x => x == elem);
            }
            tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics() ?? new List<DayTask>();
        }

        //Свернуть окно
        private void Window_StateChanged (object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            CalendarInfo.TaskList.SaveTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
        }

        //Открытие меню трея
        private void TaskbarIcon_TrayRightMouseUp (object sender, RoutedEventArgs e)
        {
            if (sender is TaskbarIcon icon)
            {
                icon.ContextMenu.IsOpen = true;
            }
        }

        //Закрытие приложения
        private void Exit_MouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CalendarInfo.TaskList.SaveTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            System.Windows.Application.Current.Shutdown();
        }

        //Развернуть окно из трея
        private void TaskbarIcon_TrayLeftMouseUp (object sender, RoutedEventArgs e)
        {
            CalendarInfo.TaskList.LoadTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics();
            Show();
            WindowState = WindowState.Normal;
        }

        //Показать коротку форму добавления задачи
        private void Label_MouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CalendarInfo.ShortForm?.Close();
            CalendarInfo.ShortForm = new ShortTaskWindow.ShortAddTaskWindow();
            CalendarInfo.ShortForm.Show();
        }

        //Загрузка при активации окна
        private void Window_Activated (object sender, EventArgs e)
        {
            CalendarInfo.TaskList.LoadTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));
            tasksGrid.ItemsSource = CalendarInfo.SelectActiveTopics();
            CalendarInfo.CurrentDate = calendar.SelectedDate ?? calendar.DisplayDate;
        }

        //Сохранение при диактивации окна
        private void Window_Deactivated (object sender, EventArgs e) => CalendarInfo.TaskList.SaveTaskList(TaskList.DateToJsonFileName(CalendarInfo.CurrentDate));

        //Загрузка текста про задачи на текущий день
        private void TodayTaskPopUp_Opened (object sender, EventArgs e)
        {
            CalendarInfo.TaskList.LoadTaskList(TaskList.DateToJsonFileName(System.DateTime.Today));

            var popup = FindResource("TodayTaskPopUp") as Popup;
            var stackPanel = new StackPanel();

            var stringBuilder = new StringBuilder();

            foreach (var task in CalendarInfo.TaskList.Tasks)
            {
                stringBuilder.Append(task).Append('\n');
            }


            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            var textBlock = new TextBlock();
            textBlock.Inlines.Add(stringBuilder.ToString());
            textBlock.FontSize = 14;
            textBlock.FontStyle = FontStyles.Italic;

            stackPanel.Children.Add(textBlock);
            stackPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDBDEFF"));

            popup.Child = stackPanel;
        }
    }

    //раскрашивание строки в зависимости от типов
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, CultureInfo culture) => new SolidColorBrush(TaskTypeColors.colors[(int)value]);
        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture) => throw new Exception("The method or operation is not implemented.");
    }

    public static class TaskTypeColors
    {
        public static Color[] colors = new Color[7];

        static TaskTypeColors ()
        {
            colors[0] = (Color)ColorConverter.ConvertFromString("#CDABFF");
            colors[1] = (Color)ColorConverter.ConvertFromString("#DEF7FE");
            colors[2] = (Color)ColorConverter.ConvertFromString("#E7ECFF");
            colors[3] = (Color)ColorConverter.ConvertFromString("#C3FBD8");
            colors[4] = (Color)ColorConverter.ConvertFromString("#FDEED9");
            colors[5] = (Color)ColorConverter.ConvertFromString("#FFFADD");
            colors[6] = (Color)ColorConverter.ConvertFromString("#FFA8A3");
        }
    }
}
