using System.Windows;

namespace PharmacyApp.Views
{
    public partial class CategoryWindow : Window
    {
        public string CategoryName
        {
            get => (string)GetValue(CategoryNameProperty);
            set => SetValue(CategoryNameProperty, value);
        }
        public static readonly DependencyProperty CategoryNameProperty =
            DependencyProperty.Register("CategoryName", typeof(string), typeof(CategoryWindow),
                new PropertyMetadata("", OnPropertyChanged));

        public string CategoryDescription
        {
            get => (string)GetValue(CategoryDescriptionProperty);
            set => SetValue(CategoryDescriptionProperty, value);
        }
        public static readonly DependencyProperty CategoryDescriptionProperty =
            DependencyProperty.Register("CategoryDescription", typeof(string), typeof(CategoryWindow),
                new PropertyMetadata("", OnPropertyChanged));

        public bool CanSave
        {
            get => (bool)GetValue(CanSaveProperty);
            set => SetValue(CanSaveProperty, value);
        }
        public static readonly DependencyProperty CanSaveProperty =
            DependencyProperty.Register("CanSave", typeof(bool), typeof(CategoryWindow), new PropertyMetadata(false));

        public CategoryWindow()
        {
            InitializeComponent();
            ValidateFields();
        }

        public CategoryWindow(string name, string description) : this()
        {
            Title = "Редактирование категории";
            CategoryName = name;
            CategoryDescription = description;
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as CategoryWindow)?.ValidateFields();
        }

        private void ValidateFields()
        {
            string errors = "";
            bool valid = true;

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                errors += "• Название категории обязательно\n";
                valid = false;
            }

            txtError.Text = errors.TrimEnd();
            txtError.Visibility = string.IsNullOrEmpty(errors) ? Visibility.Collapsed : Visibility.Visible;
            CanSave = valid;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}