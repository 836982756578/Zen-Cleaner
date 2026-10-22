// Zen Cleaner - Taşınabilir Sistem ve Disk Temizleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Orientation = System.Windows.Controls.Orientation;
using ProgressBar = System.Windows.Controls.ProgressBar;

namespace ZenCleaner
{
    public class MainWindow : Window
    {
        private List<CleanCategory> _categories;
        private NotifyIcon _notifyIcon;
        private TextBlock _txtTotalSize;
        private TextBlock _txtStatus;
        private ProgressBar _progressBar;
        private StackPanel _categoryContainer;
        private Button _btnScan;
        private Button _btnClean;
        private bool _isBusy = false;

        public MainWindow()
        {
            InitializeComponent();
            InitNotifyIcon();
            LoadCategories();
        }

        private void InitializeComponent()
        {
            Title = "Zen Cleaner";
            Width = 620;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanMinimize;

            var outerBorder = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#121216")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#2C2C3A")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hero Banner
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Categories
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Actions & Progress
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // --- HEADER ---
            var header = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#181820")),
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(18, 12, 14, 12),
                BorderBrush = new SolidColorBrush(ColorFromHex("#252532")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            header.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconText = new TextBlock
            {
                Text = "☯️",
                FontSize = 22,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var titleInfo = new StackPanel();
            var titleText = new TextBlock
            {
                Text = "Zen Cleaner",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                FontFamily = new FontFamily("Segoe UI")
            };
            var subTitleText = new TextBlock
            {
                Text = "Taşınabilir Sistem ve Disk Temizleyici",
                Foreground = new SolidColorBrush(ColorFromHex("#8E8EA8")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI")
            };
            titleInfo.Children.Add(titleText);
            titleInfo.Children.Add(subTitleText);
            titleStack.Children.Add(iconText);
            titleStack.Children.Add(titleInfo);

            var controlStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnMin = CreateTitleButton("—", (s, e) => WindowState = WindowState.Minimized);
            var btnClose = CreateTitleButton("✕", (s, e) => Close());
            controlStack.Children.Add(btnMin);
            controlStack.Children.Add(btnClose);

            Grid.SetColumn(titleStack, 0);
            Grid.SetColumn(controlStack, 1);
            headerGrid.Children.Add(titleStack);
            headerGrid.Children.Add(controlStack);
            header.Child = headerGrid;
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // --- HERO BANNER ---
            var heroBorder = new Border
            {
                Margin = new Thickness(18, 16, 18, 12),
                Padding = new Thickness(20, 16, 20, 16),
                Background = new SolidColorBrush(ColorFromHex("#1A1A24")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#2C2C3D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };
            var heroStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            _txtTotalSize = new TextBlock
            {
                Text = "0 MB",
                Foreground = new SolidColorBrush(ColorFromHex("#10B981")),
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _txtStatus = new TextBlock
            {
                Text = "Sistem taranmaya hazır. 'Sistemi Tara' butonuna tıklayın.",
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            heroStack.Children.Add(_txtTotalSize);
            heroStack.Children.Add(_txtStatus);
            heroBorder.Child = heroStack;
            Grid.SetRow(heroBorder, 1);
            mainGrid.Children.Add(heroBorder);

            // --- CATEGORIES LIST ---
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(18, 0, 18, 10)
            };
            _categoryContainer = new StackPanel();
            scroll.Content = _categoryContainer;
            Grid.SetRow(scroll, 2);
            mainGrid.Children.Add(scroll);

            // --- ACTIONS & PROGRESS ---
            var actionPanel = new StackPanel
            {
                Margin = new Thickness(18, 0, 18, 12)
            };

            _progressBar = new ProgressBar
            {
                Height = 6,
                Margin = new Thickness(0, 0, 0, 12),
                Background = new SolidColorBrush(ColorFromHex("#22222E")),
                Foreground = new SolidColorBrush(ColorFromHex("#10B981")),
                BorderThickness = new Thickness(0),
                Visibility = Visibility.Collapsed,
                IsIndeterminate = true
            };
            actionPanel.Children.Add(_progressBar);

            var buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnScan = new Button
            {
                Content = "🔍 Sistemi Tara",
                Height = 44,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(ColorFromHex("#3B82F6")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _btnScan.Click += BtnScan_Click;
            ApplyRoundedButtonStyle(_btnScan, "#3B82F6", "#2563EB");

            _btnClean = new Button
            {
                Content = "🧹 Hemen Temizle",
                Height = 44,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(ColorFromHex("#10B981")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _btnClean.Click += BtnClean_Click;
            ApplyRoundedButtonStyle(_btnClean, "#10B981", "#059669");

            Grid.SetColumn(_btnScan, 0);
            Grid.SetColumn(_btnClean, 2);
            buttonGrid.Children.Add(_btnScan);
            buttonGrid.Children.Add(_btnClean);
            actionPanel.Children.Add(buttonGrid);

            Grid.SetRow(actionPanel, 3);
            mainGrid.Children.Add(actionPanel);

            // --- FOOTER ---
            var footer = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#14141A")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#232330")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 10, 18, 10),
                CornerRadius = new CornerRadius(0, 0, 14, 14)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var authorText = new TextBlock
            {
                Text = "Geliştirici: Serkan ŞENTÜRK | © 2026 Tüm Hakları Saklıdır.",
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            authorText.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };

            var verText = new TextBlock
            {
                Text = "Zen Cleaner v1.0.0",
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(authorText, 0);
            Grid.SetColumn(verText, 1);
            footerGrid.Children.Add(authorText);
            footerGrid.Children.Add(verText);
            footer.Child = footerGrid;

            Grid.SetRow(footer, 4);
            mainGrid.Children.Add(footer);

            outerBorder.Child = mainGrid;
            Content = outerBorder;
        }

        private void LoadCategories()
        {
            _categories = CleanerEngine.GetCategories();
            _categoryContainer.Children.Clear();

            foreach (var cat in _categories)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(ColorFromHex("#171720")),
                    BorderBrush = new SolidColorBrush(ColorFromHex("#262635")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(14, 10, 14, 10)
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Checkbox
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Size

                var chk = new CheckBox
                {
                    IsChecked = cat.IsSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                chk.Checked += (s, e) => { cat.IsSelected = true; UpdateTotalSizeDisplay(); };
                chk.Unchecked += (s, e) => { cat.IsSelected = false; UpdateTotalSizeDisplay(); };

                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var title = new TextBlock
                {
                    Text = cat.Title,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13
                };
                var desc = new TextBlock
                {
                    Text = cat.Description,
                    Foreground = new SolidColorBrush(ColorFromHex("#7E7E94")),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                infoStack.Children.Add(title);
                infoStack.Children.Add(desc);

                var txtSize = new TextBlock
                {
                    Text = cat.GetFormattedSize(),
                    Foreground = new SolidColorBrush(ColorFromHex("#10B981")),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = cat.Id
                };

                Grid.SetColumn(chk, 0);
                Grid.SetColumn(infoStack, 1);
                Grid.SetColumn(txtSize, 2);

                itemGrid.Children.Add(chk);
                itemGrid.Children.Add(infoStack);
                itemGrid.Children.Add(txtSize);
                card.Child = itemGrid;

                _categoryContainer.Children.Add(card);
            }
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true);
            _txtStatus.Text = "Sistem taranıyor, gereksiz dosyalar analiz ediliyor...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                foreach (var cat in _categories)
                {
                    CleanerEngine.ScanCategory(cat);
                }

                Dispatcher.Invoke((Action)(() =>
                {
                    RefreshCategorySizes();
                    UpdateTotalSizeDisplay();
                    SetBusy(false);
                    _txtStatus.Text = "Tarama tamamlandı. Temizlemek için 'Hemen Temizle' butonuna basın.";
                }));
            });
        }

        private void BtnClean_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            SetBusy(true);
            _txtStatus.Text = "Temizlik yapılıyor, lütfen bekleyin...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                long totalFreed = 0;
                foreach (var cat in _categories)
                {
                    if (cat.IsSelected)
                    {
                        totalFreed += CleanerEngine.CleanCategory(cat);
                        // Re-scan category after cleaning
                        CleanerEngine.ScanCategory(cat);
                    }
                }

                Dispatcher.Invoke((Action)(() =>
                {
                    RefreshCategorySizes();
                    UpdateTotalSizeDisplay();
                    SetBusy(false);
                    _txtStatus.Text = "Temizlik başarıyla tamamlandı! " + CleanerEngine.FormatSize(totalFreed) + " disk alanı kurtarıldı.";
                }));
            });
        }

        private void RefreshCategorySizes()
        {
            foreach (var child in _categoryContainer.Children)
            {
                var card = child as Border;
                if (card == null) continue;
                var grid = card.Child as Grid;
                if (grid == null) continue;

                foreach (var element in grid.Children)
                {
                    var txt = element as TextBlock;
                    if (txt != null && txt.Tag != null)
                    {
                        string id = txt.Tag.ToString();
                        foreach (var cat in _categories)
                        {
                            if (cat.Id == id)
                            {
                                txt.Text = cat.GetFormattedSize();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateTotalSizeDisplay()
        {
            long total = 0;
            foreach (var cat in _categories)
            {
                if (cat.IsSelected)
                {
                    total += cat.BytesFound;
                }
            }
            _txtTotalSize.Text = CleanerEngine.FormatSize(total);
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            _progressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            _btnScan.IsEnabled = !busy;
            _btnClean.IsEnabled = !busy;
        }

        private Button CreateTitleButton(string text, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 28,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0)
            };
            btn.Click += onClick;
            return btn;
        }

        private void ApplyRoundedButtonStyle(Button btn, string normalHex, string hoverHex)
        {
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var elem = new FrameworkElementFactory(typeof(Border));
            elem.Name = "border";
            elem.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            elem.SetValue(Border.BackgroundProperty, new SolidColorBrush(ColorFromHex(normalHex)));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            elem.AppendChild(presenter);

            template.VisualTree = elem;

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(ColorFromHex(hoverHex)), "border"));
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            btn.Style = style;
        }

        private void InitNotifyIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                string exePath = Assembly.GetExecutingAssembly().Location;
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? System.Drawing.SystemIcons.Application;
                _notifyIcon.Text = "Zen Cleaner - Sistem Temizleyici";
                _notifyIcon.Visible = true;

                var menu = new ContextMenuStrip();
                menu.Items.Add("🔍 Göster / Gizle", null, (s, e) => ToggleWindow());
                menu.Items.Add("🧹 Hızlı Temizle", null, (s, e) => { Show(); BtnClean_Click(null, null); });
                menu.Items.Add("-");
                menu.Items.Add("🌐 Geliştirici Serkan ŞENTÜRK", null, (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
                });
                menu.Items.Add("✕ Çıkış", null, (s, e) =>
                {
                    _notifyIcon.Visible = false;
                    Application.Current.Shutdown();
                });

                _notifyIcon.ContextMenuStrip = menu;
                _notifyIcon.DoubleClick += (s, e) => ToggleWindow();
            }
            catch { }
        }

        private void ToggleWindow()
        {
            if (WindowState == WindowState.Minimized || Visibility != Visibility.Visible)
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
