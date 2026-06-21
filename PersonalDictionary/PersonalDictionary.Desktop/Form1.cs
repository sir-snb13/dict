using System.Text;
using PersonalDictionary.Core;

namespace PersonalDictionary.Desktop;

public partial class Form1 : Form
{
    private readonly TranslationService translationService = new();
    private readonly WordEntryStorageService wordEntryStorageService;
    private readonly TrainingStatisticsStorageService trainingStatisticsStorageService;
    private readonly TrainingSession trainingSession = new();
    private readonly List<WordEntry> wordEntries = new();
    private readonly List<TrainingStatistics> trainingStatistics = new();

    private readonly Color backgroundColor = Color.FromArgb(245, 247, 250);
    private readonly Color cardColor = Color.White;
    private readonly Color accentColor = Color.FromArgb(37, 99, 235);
    private readonly Color accentHoverColor = Color.FromArgb(29, 78, 216);
    private readonly Color dangerColor = Color.FromArgb(220, 38, 38);
    private readonly Color textColor = Color.FromArgb(31, 41, 55);
    private readonly Color mutedTextColor = Color.FromArgb(107, 114, 128);

    private readonly TextBox wordTextBox = new ModernTextBox();
    private readonly TextBox translationTextBox = new ModernTextBox();
    private readonly TextBox categoryTextBox = new ModernTextBox();
    private readonly TextBox searchTextBox = new ModernTextBox();
    private readonly TextBox trainingAnswerTextBox = new ModernTextBox();

    private readonly Button autoTranslateButton = new RoundedButton();
    private readonly Button addButton = new RoundedButton();
    private readonly Button deleteButton = new RoundedButton();
    private readonly Button exportButton = new RoundedButton();
    private readonly Button importButton = new RoundedButton();
    private readonly CheckBox autoTranslateImportCheckBox = new();
    private readonly Button startTrainingButton = new RoundedButton();
    private readonly Button checkAnswerButton = new RoundedButton();
    private readonly Button nextQuestionButton = new RoundedButton();

    private readonly DataGridView wordsGrid = new();
    private readonly DataGridView trainingStatisticsGrid = new();
    private readonly ComboBox trainingModeComboBox = new();
    private readonly Label trainingQuestionLabel = new();
    private readonly Label trainingResultLabel = new();
    private readonly Label trainingStatsLabel = new();
    private readonly Label totalTrainingsHistoryLabel = new();
    private readonly Label totalQuestionsHistoryLabel = new();
    private readonly Label correctAnswersHistoryLabel = new();
    private readonly Label averageAccuracyHistoryLabel = new();
    private readonly ToolStripStatusLabel wordCountStatusLabel = new();
    private readonly ToolStripStatusLabel actionStatusLabel = new();

    private TabControl mainTabControl = null!;
    private TabPage dictionaryTabPage = null!;
    private TrainingQuestion? currentQuestion;
    private TrainingStatistics? currentTrainingStatistics;

    public Form1()
    {
        wordEntryStorageService = new WordEntryStorageService(AppContext.BaseDirectory);

        var previousFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalDictionary",
            "words.json");
        wordEntryStorageService.MigrateFrom(previousFilePath);
        wordEntries.AddRange(wordEntryStorageService.LoadEntries());

        trainingStatisticsStorageService = new TrainingStatisticsStorageService(AppContext.BaseDirectory);
        trainingStatistics.AddRange(trainingStatisticsStorageService.LoadStatistics());

        InitializeComponent();
        BuildInterface();
        RefreshWordsGrid();
        UpdateStatus(wordEntries.Count == 0
            ? "Готово. Добавьте первое слово."
            : $"Загружено слов: {wordEntries.Count}.");
        UpdateTrainingStats();
        RefreshTrainingHistory();

        FormClosing += (_, _) =>
        {
            TrySaveWordEntries(showError: false);
            TrySaveTrainingStatistics(showError: false);
        };
    }

    private void BuildInterface()
    {
        Text = "Личный словарь";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1220, 780);
        MinimumSize = new Size(1120, 720);
        BackColor = backgroundColor;
        Font = new Font("Segoe UI", 10F);
        KeyPreview = true;

        Controls.Clear();

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(26),
            BackColor = backgroundColor
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        dictionaryTabPage = CreateDictionaryTab();
        mainTabControl = CreateMainTabControl();
        mainTabControl.TabPages.Add(dictionaryTabPage);
        mainTabControl.TabPages.Add(CreateTrainingTab());
        mainTabControl.TabPages.Add(CreateStatisticsTab());
        mainTabControl.TabPages.Add(CreateImportExportTab());

        mainLayout.Controls.Add(CreateHeaderPanel(), 0, 0);
        mainLayout.Controls.Add(mainTabControl, 0, 1);
        mainLayout.Controls.Add(CreateStatusStrip(), 0, 2);

        Controls.Add(mainLayout);
    }

    private Panel CreateHeaderPanel()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = accentColor,
            BorderColor = accentColor,
            BorderRadius = 8,
            BorderSize = 0,
            Padding = new Padding(32, 14, 32, 14)
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Text = "Личный словарь иностранных слов",
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(titleLabel);

        return panel;
    }

    private TabControl CreateMainTabControl()
    {
        return new ModernTabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            ItemSize = new Size(220, 46),
            AccentColor = accentColor,
            SurfaceColor = cardColor,
            TextColor = textColor
        };
    }

    private TabPage CreateDictionaryTab()
    {
        var tabPage = new TabPage("Словарь")
        {
            BackColor = backgroundColor,
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateWordFormPanel(), 0, 0);
        layout.Controls.Add(CreateWordsTablePanel(), 1, 0);

        tabPage.Controls.Add(layout);

        return tabPage;
    }

    private Panel CreateWordFormPanel()
    {
        var panel = CreateCardPanel(new Padding(28));
        panel.Margin = new Padding(0, 0, 22, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 14
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        wordTextBox.Dock = DockStyle.Fill;
        wordTextBox.Font = new Font("Segoe UI", 11F);
        wordTextBox.Margin = new Padding(0);
        wordTextBox.PlaceholderText = "например: книга";

        translationTextBox.Dock = DockStyle.Fill;
        translationTextBox.Font = new Font("Segoe UI", 11F);
        translationTextBox.Margin = new Padding(0);
        translationTextBox.PlaceholderText = "например: book";

        categoryTextBox.Dock = DockStyle.Fill;
        categoryTextBox.Font = new Font("Segoe UI", 11F);
        categoryTextBox.Margin = new Padding(0);
        categoryTextBox.PlaceholderText = "например: существительные";

        StyleButton(autoTranslateButton, "Автоперевод", accentColor, Color.White);
        autoTranslateButton.Click += AutoTranslateButton_Click;

        StyleButton(addButton, "Добавить", accentColor, Color.White);
        addButton.Dock = DockStyle.Fill;
        addButton.Margin = new Padding(0);
        addButton.Click += AddButton_Click;

        StyleButton(deleteButton, "Удалить выбранное", dangerColor, Color.White);
        deleteButton.Dock = DockStyle.Fill;
        deleteButton.Margin = new Padding(0);
        deleteButton.Click += DeleteButton_Click;

        layout.Controls.Add(CreateSectionTitle("Новое слово"), 0, 0);
        layout.Controls.Add(CreateFieldLabel("Слово (русский)"), 0, 1);
        layout.Controls.Add(wordTextBox, 0, 2);
        layout.Controls.Add(CreateVerticalSpacer(14), 0, 3);
        layout.Controls.Add(CreateFieldLabel("Перевод (английский)"), 0, 4);
        layout.Controls.Add(CreateTranslationInputBlock(), 0, 5);
        layout.Controls.Add(CreateVerticalSpacer(14), 0, 6);
        layout.Controls.Add(CreateFieldLabel("Категория"), 0, 7);
        layout.Controls.Add(categoryTextBox, 0, 8);
        layout.Controls.Add(CreateVerticalSpacer(24), 0, 9);
        layout.Controls.Add(addButton, 0, 10);
        layout.Controls.Add(CreateVerticalSpacer(12), 0, 11);
        layout.Controls.Add(deleteButton, 0, 12);

        panel.Controls.Add(layout);

        return panel;
    }

    private TableLayoutPanel CreateTranslationInputBlock()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Height = 44
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 162));

        autoTranslateButton.Dock = DockStyle.Fill;
        autoTranslateButton.Margin = new Padding(12, 0, 0, 0);
        translationTextBox.Margin = new Padding(0);

        layout.Controls.Add(translationTextBox, 0, 0);
        layout.Controls.Add(autoTranslateButton, 1, 0);

        return layout;
    }

    private Panel CreateWordsTablePanel()
    {
        var panel = CreateCardPanel(new Padding(24));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        searchTextBox.Dock = DockStyle.Fill;
        searchTextBox.Font = new Font("Segoe UI", 11F);
        searchTextBox.Margin = new Padding(0);
        searchTextBox.PlaceholderText = "Поиск по слову, переводу или категории";
        searchTextBox.TextChanged += SearchTextBox_TextChanged;

        ConfigureWordsGrid();

        layout.Controls.Add(CreateSectionTitle("Список слов"), 0, 0);
        layout.Controls.Add(searchTextBox, 0, 1);
        layout.Controls.Add(CreateVerticalSpacer(18), 0, 2);
        layout.Controls.Add(wordsGrid, 0, 3);

        panel.Controls.Add(layout);

        return panel;
    }

    private TabPage CreateTrainingTab()
    {
        var tabPage = new TabPage("Тренировка")
        {
            BackColor = backgroundColor,
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

        layout.Controls.Add(CreateTrainingMainPanel(), 0, 0);
        layout.Controls.Add(CreateTrainingStatsPanel(), 1, 0);

        tabPage.Controls.Add(layout);

        return tabPage;
    }

    private Panel CreateTrainingMainPanel()
    {
        var panel = CreateCardPanel(new Padding(28));
        panel.Margin = new Padding(0, 0, 22, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        trainingModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        trainingModeComboBox.Items.Add("Слово → перевод");
        trainingModeComboBox.Items.Add("Перевод → слово");
        trainingModeComboBox.SelectedIndex = 0;
        trainingModeComboBox.Dock = DockStyle.Left;
        trainingModeComboBox.Font = new Font("Segoe UI", 11F);
        trainingModeComboBox.Width = 250;

        StyleButton(startTrainingButton, "Начать тренировку", accentColor, Color.White);
        startTrainingButton.Width = 210;
        startTrainingButton.Click += StartTrainingButton_Click;

        var topActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0),
            WrapContents = false
        };

        topActions.Controls.Add(trainingModeComboBox);
        topActions.Controls.Add(startTrainingButton);

        trainingQuestionLabel.Dock = DockStyle.Fill;
        trainingQuestionLabel.BackColor = Color.Transparent;
        trainingQuestionLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        trainingQuestionLabel.ForeColor = textColor;
        trainingQuestionLabel.Text = "Нажмите «Начать тренировку»";
        trainingQuestionLabel.TextAlign = ContentAlignment.MiddleCenter;

        trainingAnswerTextBox.Dock = DockStyle.Fill;
        trainingAnswerTextBox.Font = new Font("Segoe UI", 14F);
        trainingAnswerTextBox.Margin = new Padding(0);
        trainingAnswerTextBox.PlaceholderText = "Введите ответ";

        StyleButton(checkAnswerButton, "Проверить", accentColor, Color.White);
        StyleButton(nextQuestionButton, "Следующее слово", Color.FromArgb(55, 65, 81), Color.White);
        checkAnswerButton.Width = 170;
        nextQuestionButton.Width = 210;
        checkAnswerButton.Enabled = false;
        nextQuestionButton.Enabled = false;
        checkAnswerButton.Click += CheckAnswerButton_Click;
        nextQuestionButton.Click += NextQuestionButton_Click;

        var answerButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false
        };

        answerButtons.Controls.Add(checkAnswerButton);
        answerButtons.Controls.Add(nextQuestionButton);

        trainingResultLabel.Dock = DockStyle.Fill;
        trainingResultLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        trainingResultLabel.ForeColor = mutedTextColor;
        trainingResultLabel.TextAlign = ContentAlignment.MiddleLeft;

        layout.Controls.Add(CreateSectionTitle("Тренировка слов"), 0, 0);
        layout.Controls.Add(topActions, 0, 1);
        layout.Controls.Add(CreateTrainingQuestionCard(), 0, 2);
        layout.Controls.Add(CreateFieldLabel("Ваш ответ"), 0, 3);
        layout.Controls.Add(trainingAnswerTextBox, 0, 4);
        layout.Controls.Add(answerButtons, 0, 5);
        layout.Controls.Add(trainingResultLabel, 0, 6);

        panel.Controls.Add(layout);

        return panel;
    }

    private Panel CreateTrainingQuestionCard()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(239, 246, 255),
            BorderColor = Color.FromArgb(191, 219, 254),
            BorderRadius = 8,
            BorderSize = 1,
            Padding = new Padding(18)
        };

        panel.Controls.Add(trainingQuestionLabel);

        return panel;
    }

    private Panel CreateTrainingStatsPanel()
    {
        var panel = CreateCardPanel(new Padding(24));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        trainingStatsLabel.Dock = DockStyle.Fill;
        trainingStatsLabel.Font = new Font("Segoe UI", 12F);
        trainingStatsLabel.ForeColor = textColor;
        trainingStatsLabel.TextAlign = ContentAlignment.TopLeft;

        layout.Controls.Add(CreateSectionTitle("Статистика"), 0, 0);
        layout.Controls.Add(trainingStatsLabel, 0, 1);

        panel.Controls.Add(layout);

        return panel;
    }

    private TabPage CreateStatisticsTab()
    {
        var tabPage = new TabPage("Статистика")
        {
            BackColor = backgroundColor,
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var summaryPanel = CreateCardPanel(new Padding(24));
        summaryPanel.Margin = new Padding(0, 0, 0, 18);

        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };
        for (var column = 0; column < 4; column++)
        {
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        AddHistoryMetric(summaryLayout, 0, "Всего тренировок", totalTrainingsHistoryLabel);
        AddHistoryMetric(summaryLayout, 1, "Всего вопросов", totalQuestionsHistoryLabel);
        AddHistoryMetric(summaryLayout, 2, "Правильных ответов", correctAnswersHistoryLabel);
        AddHistoryMetric(summaryLayout, 3, "Общая точность", averageAccuracyHistoryLabel);
        summaryPanel.Controls.Add(summaryLayout);

        var historyPanel = CreateCardPanel(new Padding(24));
        ConfigureTrainingStatisticsGrid();
        historyPanel.Controls.Add(trainingStatisticsGrid);

        layout.Controls.Add(summaryPanel, 0, 0);
        layout.Controls.Add(historyPanel, 0, 1);
        tabPage.Controls.Add(layout);

        return tabPage;
    }

    private void AddHistoryMetric(
        TableLayoutPanel layout,
        int column,
        string caption,
        Label valueLabel)
    {
        var captionLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = caption,
            Font = new Font("Segoe UI", 10F),
            ForeColor = mutedTextColor,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 0, 12, 4)
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        valueLabel.ForeColor = textColor;
        valueLabel.TextAlign = ContentAlignment.TopLeft;
        valueLabel.Margin = new Padding(0, 0, 12, 0);

        layout.Controls.Add(captionLabel, column, 0);
        layout.Controls.Add(valueLabel, column, 1);
    }

    private void ConfigureTrainingStatisticsGrid()
    {
        trainingStatisticsGrid.AutoGenerateColumns = false;
        trainingStatisticsGrid.AllowUserToAddRows = false;
        trainingStatisticsGrid.AllowUserToDeleteRows = false;
        trainingStatisticsGrid.AllowUserToResizeRows = false;
        trainingStatisticsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        trainingStatisticsGrid.BackgroundColor = cardColor;
        trainingStatisticsGrid.BorderStyle = BorderStyle.None;
        trainingStatisticsGrid.Dock = DockStyle.Fill;
        trainingStatisticsGrid.EnableHeadersVisualStyles = false;
        trainingStatisticsGrid.GridColor = Color.FromArgb(229, 231, 235);
        trainingStatisticsGrid.MultiSelect = false;
        trainingStatisticsGrid.ReadOnly = true;
        trainingStatisticsGrid.RowHeadersVisible = false;
        trainingStatisticsGrid.RowTemplate.Height = 38;
        trainingStatisticsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        trainingStatisticsGrid.ColumnHeadersDefaultCellStyle.BackColor = accentColor;
        trainingStatisticsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        trainingStatisticsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        trainingStatisticsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        trainingStatisticsGrid.DefaultCellStyle.SelectionForeColor = textColor;
        trainingStatisticsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Дата",
            DataPropertyName = nameof(TrainingHistoryRow.Date),
            FillWeight = 115
        });
        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Режим",
            DataPropertyName = nameof(TrainingHistoryRow.Mode),
            FillWeight = 145
        });
        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Вопросы",
            DataPropertyName = nameof(TrainingHistoryRow.TotalQuestions),
            FillWeight = 70
        });
        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Правильные",
            DataPropertyName = nameof(TrainingHistoryRow.CorrectAnswers),
            FillWeight = 80
        });
        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ошибки",
            DataPropertyName = nameof(TrainingHistoryRow.WrongAnswers),
            FillWeight = 65
        });
        trainingStatisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Точность",
            DataPropertyName = nameof(TrainingHistoryRow.Accuracy),
            FillWeight = 75
        });
    }
    private TabPage CreateImportExportTab()
    {
        var tabPage = new TabPage("Импорт/экспорт")
        {
            BackColor = backgroundColor,
            Padding = new Padding(18)
        };

        var panel = CreateCardPanel(new Padding(28));
        panel.Dock = DockStyle.Top;
        panel.Height = 230;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        StyleButton(exportButton, "Экспорт в CSV", accentColor, Color.White);
        StyleButton(importButton, "Импорт из CSV", Color.FromArgb(55, 65, 81), Color.White);
        exportButton.Width = 210;
        importButton.Width = 210;
        exportButton.Click += ExportButton_Click;
        importButton.Click += ImportButton_Click;

        autoTranslateImportCheckBox.AutoSize = true;
        autoTranslateImportCheckBox.Text = "Автоперевод одной колонки";
        autoTranslateImportCheckBox.ForeColor = textColor;
        autoTranslateImportCheckBox.BackColor = Color.Transparent;
        autoTranslateImportCheckBox.Margin = new Padding(12, 11, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0),
            WrapContents = false
        };

        buttons.Controls.Add(exportButton);
        buttons.Controls.Add(importButton);
        buttons.Controls.Add(autoTranslateImportCheckBox);

        var descriptionLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = mutedTextColor,
            Text = "Без флага импортируются колонки Word, Translation, Category. С флагом одна колонка русских/английских слов переводится автоматически.",
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(CreateSectionTitle("Импорт и экспорт словаря"), 0, 0);
        layout.Controls.Add(descriptionLabel, 0, 1);
        layout.Controls.Add(CreateVerticalSpacer(16), 0, 2);
        layout.Controls.Add(buttons, 0, 3);

        panel.Controls.Add(layout);
        tabPage.Controls.Add(panel);

        return tabPage;
    }

    private void ConfigureWordsGrid()
    {
        wordsGrid.AllowUserToAddRows = false;
        wordsGrid.AllowUserToDeleteRows = false;
        wordsGrid.AllowUserToResizeRows = false;
        wordsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        wordsGrid.BackgroundColor = cardColor;
        wordsGrid.BorderStyle = BorderStyle.None;
        wordsGrid.ColumnHeadersHeight = 42;
        wordsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        wordsGrid.Dock = DockStyle.Fill;
        wordsGrid.EnableHeadersVisualStyles = false;
        wordsGrid.GridColor = Color.FromArgb(229, 231, 235);
        wordsGrid.MultiSelect = false;
        wordsGrid.ReadOnly = true;
        wordsGrid.RowHeadersVisible = false;
        wordsGrid.RowTemplate.Height = 38;
        wordsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        wordsGrid.ColumnHeadersDefaultCellStyle.BackColor = accentColor;
        wordsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        wordsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        wordsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = accentColor;

        wordsGrid.DefaultCellStyle.BackColor = Color.White;
        wordsGrid.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);
        wordsGrid.DefaultCellStyle.ForeColor = textColor;
        wordsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        wordsGrid.DefaultCellStyle.SelectionForeColor = textColor;
        wordsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

        wordsGrid.Columns.Clear();
        wordsGrid.Columns.Add("WordColumn", "Слово (русский)");
        wordsGrid.Columns.Add("TranslationColumn", "Перевод (английский)");
        wordsGrid.Columns.Add("CategoryColumn", "Категория");
    }

    private StatusStrip CreateStatusStrip()
    {
        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            BackColor = cardColor,
            SizingGrip = false
        };

        wordCountStatusLabel.ForeColor = textColor;
        actionStatusLabel.ForeColor = mutedTextColor;
        actionStatusLabel.Spring = true;
        actionStatusLabel.TextAlign = ContentAlignment.MiddleRight;

        statusStrip.Items.Add(wordCountStatusLabel);
        statusStrip.Items.Add(actionStatusLabel);

        return statusStrip;
    }

    private Panel CreateCardPanel(Padding padding)
    {
        return new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = cardColor,
            BorderColor = Color.FromArgb(229, 231, 235),
            BorderRadius = 8,
            BorderSize = 1,
            Padding = padding
        };
    }

    private Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = textColor,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = mutedTextColor,
            Margin = new Padding(0),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Panel CreateVerticalSpacer(int height)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Height = height
        };
    }

    private void StyleButton(Button button, string text, Color backColor, Color foreColor)
    {
        button.Text = text;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        button.Height = 40;
        button.Width = 170;
        button.Margin = new Padding(0, 0, 10, 8);
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;

        if (button is RoundedButton roundedButton)
        {
            roundedButton.BorderRadius = 8;
            roundedButton.HoverBackColor = backColor == dangerColor
                ? Color.FromArgb(185, 28, 28)
                : accentHoverColor;
            roundedButton.PressedBackColor = backColor == dangerColor
                ? Color.FromArgb(153, 27, 27)
                : Color.FromArgb(30, 64, 175);
        }
    }

    private void RefreshWordsGrid()
    {
        var filter = searchTextBox.Text.Trim();

        wordsGrid.Rows.Clear();

        foreach (var entry in wordEntries.Where(entry => MatchesFilter(entry, filter)))
        {
            var rowIndex = wordsGrid.Rows.Add(entry.Word, entry.Translation, entry.Category);
            wordsGrid.Rows[rowIndex].Tag = entry;
        }

        UpdateWordCountStatus();
    }

    private static bool MatchesFilter(WordEntry entry, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return entry.Word.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.Translation.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.Category.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateWordCountStatus()
    {
        wordCountStatusLabel.Text = $"Слов: {wordEntries.Count}";
    }

    private void UpdateStatus(string actionText)
    {
        UpdateWordCountStatus();
        actionStatusLabel.Text = actionText;
    }

    private bool TrySaveWordEntries(bool showError = true)
    {
        try
        {
            wordEntryStorageService.SaveEntries(wordEntries);
            return true;
        }
        catch
        {
            if (showError)
            {
                MessageBox.Show(
                    this,
                    "Не удалось сохранить словарь в JSON.",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            UpdateStatus("Словарь не сохранён.");
            return false;
        }
    }

    private bool TrySaveTrainingStatistics(bool showError = true)
    {
        try
        {
            trainingStatisticsStorageService.SaveStatistics(trainingStatistics);
            return true;
        }
        catch
        {
            if (showError)
            {
                MessageBox.Show(
                    this,
                    "Не удалось сохранить статистику тренировок в JSON.",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            UpdateStatus("Статистика тренировок не сохранена.");
            return false;
        }
    }
    private void AddButton_Click(object? sender, EventArgs e)
    {
        AddWordFromFields();
    }

    private void AddWordFromFields()
    {
        var word = wordTextBox.Text.Trim();
        var translation = translationTextBox.Text.Trim();
        var category = categoryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
        {
            MessageBox.Show(
                this,
                "Заполните поля \"Слово\" и \"Перевод\".",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        WordEntry entry;

        try
        {
            entry = TranslationService.CreateNormalizedWordEntry(word, translation, category);
        }
        catch (ArgumentException)
        {
            MessageBox.Show(
                this,
                "Поле \"Слово\" должно содержать русское слово, а поле \"Перевод\" — английское. Если поля заполнены наоборот, программа поменяет их местами автоматически.",
                "Проверьте языки",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (ContainsWordEntry(entry))
        {
            MessageBox.Show(
                this,
                "Такая пара слов уже есть в словаре.",
                "Дубликат",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        wordEntries.Add(entry);

        if (!TrySaveWordEntries())
        {
            wordEntries.Remove(entry);
            return;
        }

        RefreshWordsGrid();
        ClearWordInputFields();
        wordTextBox.Focus();
        UpdateStatus("Слово добавлено и сохранено: русский → английский.");
    }
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (wordsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                this,
                "Выберите слово для удаления.",
                "Удаление",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (wordsGrid.SelectedRows[0].Tag is WordEntry selectedEntry)
        {
            var removedIndex = wordEntries.IndexOf(selectedEntry);
            wordEntries.Remove(selectedEntry);

            if (!TrySaveWordEntries())
            {
                wordEntries.Insert(removedIndex, selectedEntry);
                RefreshWordsGrid();
                return;
            }

            RefreshWordsGrid();
            UpdateStatus("Слово удалено из словаря и JSON.");
        }
    }

    private async void AutoTranslateButton_Click(object? sender, EventArgs e)
    {
        var sourceText = wordTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            MessageBox.Show(
                this,
                "Введите слово для перевода.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        autoTranslateButton.Enabled = false;
        autoTranslateButton.Text = "Перевод...";
        UpdateStatus("Определяется язык и выполняется перевод...");

        try
        {
            var entry = await translationService.CreateWordEntryAsync(
                sourceText,
                categoryTextBox.Text.Trim());

            wordTextBox.Text = entry.Word;
            translationTextBox.Text = entry.Translation;
            UpdateStatus("Поля приведены к формату: русский → английский.");
        }
        catch (ArgumentException)
        {
            MessageBox.Show(
                this,
                "Введите слово только на русском или только на английском языке.",
                "Не удалось определить язык",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            UpdateStatus("Язык слова не определён.");
        }
        catch
        {
            MessageBox.Show(
                this,
                "Не удалось получить перевод. Введите перевод вручную.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            UpdateStatus("Автоперевод недоступен.");
        }
        finally
        {
            autoTranslateButton.Text = "Автоперевод";
            autoTranslateButton.Enabled = true;
        }
    }
    private void SearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        RefreshWordsGrid();
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        ExportToCsv();
    }

    private async void ImportButton_Click(object? sender, EventArgs e)
    {
        await ImportFromCsvAsync();
    }

    private void ExportToCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = "personal-dictionary.csv",
            Title = "Экспорт словаря в CSV"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var lines = new List<string> { "Word;Translation;Category" };
            lines.AddRange(wordEntries.Select(entry =>
                $"{EscapeCsvValue(entry.Word)};{EscapeCsvValue(entry.Translation)};{EscapeCsvValue(entry.Category)}"));

            File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(false));
            UpdateStatus("CSV экспортирован.");
        }
        catch
        {
            MessageBox.Show(
                this,
                "Не удалось экспортировать CSV.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            UpdateStatus("Ошибка экспорта CSV.");
        }
    }

    private async Task ImportFromCsvAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            Title = "Импорт словаря из CSV"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var useAutomaticTranslation = autoTranslateImportCheckBox.Checked;
        importButton.Enabled = false;
        autoTranslateImportCheckBox.Enabled = false;
        importButton.Text = "Импорт...";

        try
        {
            var lines = await File.ReadAllLinesAsync(dialog.FileName, Encoding.UTF8);
            var rows = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseCsvLine)
                .Where(values => !IsCsvHeader(values))
                .ToList();

            var category = Path.GetFileNameWithoutExtension(dialog.FileName).Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "Импорт CSV";
            }

            var requiredColumnCount = useAutomaticTranslation ? 1 : 3;
            var importedCount = 0;
            var skippedCount = 0;
            var translationErrorCount = 0;
            var processedCount = 0;
            var rowsToProcessCount = rows.Count(values => values.Count == requiredColumnCount);

            foreach (var values in rows)
            {
                if (values.Count != requiredColumnCount)
                {
                    skippedCount++;
                    continue;
                }

                WordEntry entry;

                if (useAutomaticTranslation)
                {
                    var sourceWord = values[0].Trim();
                    processedCount++;

                    if (string.IsNullOrWhiteSpace(sourceWord))
                    {
                        skippedCount++;
                        continue;
                    }

                    UpdateStatus($"Автоперевод: {processedCount}/{rowsToProcessCount} — {sourceWord}");

                    try
                    {
                        entry = await translationService.CreateWordEntryAsync(sourceWord, category);
                    }
                    catch (ArgumentException)
                    {
                        skippedCount++;
                        continue;
                    }
                    catch
                    {
                        translationErrorCount++;
                        continue;
                    }
                }
                else
                {
                    var word = values[0].Trim();
                    var translation = values[1].Trim();
                    var rowCategory = values[2].Trim();

                    if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        entry = TranslationService.CreateNormalizedWordEntry(
                            word,
                            translation,
                            rowCategory);
                    }
                    catch (ArgumentException)
                    {
                        skippedCount++;
                        continue;
                    }
                }

                if (ContainsWordEntry(entry))
                {
                    skippedCount++;
                    continue;
                }

                wordEntries.Add(entry);
                importedCount++;
            }

            var savedSuccessfully = importedCount == 0 || TrySaveWordEntries();
            RefreshWordsGrid();
            UpdateStatus(savedSuccessfully
                ? $"Импортировано и сохранено слов: {importedCount}."
                : "Импорт выполнен, но сохранить JSON не удалось.");

            var modeDescription = useAutomaticTranslation
                ? "одна колонка с автопереводом"
                : "готовый словарь из трёх колонок";

            MessageBox.Show(
                this,
                $"Режим: {modeDescription}{Environment.NewLine}" +
                $"Добавлено: {importedCount}{Environment.NewLine}" +
                $"Пропущено: {skippedCount}{Environment.NewLine}" +
                $"Ошибок перевода: {translationErrorCount}",
                "Импорт CSV завершён",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch
        {
            MessageBox.Show(
                this,
                "Не удалось импортировать CSV.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            UpdateStatus("Ошибка импорта CSV.");
        }
        finally
        {
            importButton.Text = "Импорт из CSV";
            importButton.Enabled = true;
            autoTranslateImportCheckBox.Enabled = true;
        }
    }
    private bool ContainsWordEntry(WordEntry candidate)
    {
        return wordEntries.Any(entry =>
            string.Equals(entry.Word, candidate.Word, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entry.Translation, candidate.Translation, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCsvHeader(IReadOnlyList<string> values)
    {
        if (values.Count == 3)
        {
            return string.Equals(values[0].Trim(), "Word", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(values[1].Trim(), "Translation", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(values[2].Trim(), "Category", StringComparison.OrdinalIgnoreCase);
        }

        if (values.Count != 1)
        {
            return false;
        }

        return values[0].Trim().ToLowerInvariant() is "word" or "words" or "слово" or "слова";
    }
    private static string EscapeCsvValue(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var currentChar = line[i];

            if (currentChar == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (currentChar == ';' && !insideQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(currentChar);
            }
        }

        if (insideQuotes)
        {
            return new List<string>();
        }

        values.Add(currentValue.ToString());

        return values;
    }

    private void StartTrainingButton_Click(object? sender, EventArgs e)
    {
        if (wordEntries.Count == 0)
        {
            MessageBox.Show(
                this,
                "Добавьте хотя бы одно слово в словарь.",
                "Тренировка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var mode = GetSelectedTrainingMode();
        trainingSession.Reset();
        currentTrainingStatistics = new TrainingStatistics
        {
            StartedAt = DateTime.Now,
            Mode = mode
        };

        CreateNextTrainingQuestion();
        UpdateTrainingStats();
        UpdateStatus("Тренировка начата.");
    }
    private void CheckAnswerButton_Click(object? sender, EventArgs e)
    {
        if (currentQuestion is null)
        {
            return;
        }

        var isCorrect = trainingSession.CheckAnswer(currentQuestion, trainingAnswerTextBox.Text);

        if (isCorrect)
        {
            trainingResultLabel.ForeColor = Color.FromArgb(22, 163, 74);
            trainingResultLabel.Text = "Правильно.";
            UpdateStatus("Ответ верный.");
        }
        else
        {
            trainingResultLabel.ForeColor = dangerColor;
            trainingResultLabel.Text = $"Неправильно. Правильный ответ: {currentQuestion.CorrectAnswer}";
            UpdateStatus("Ответ неверный.");
        }

        checkAnswerButton.Enabled = false;
        UpdateTrainingStats();
        SaveCurrentTrainingProgress();
    }
    private void NextQuestionButton_Click(object? sender, EventArgs e)
    {
        if (wordEntries.Count == 0)
        {
            MessageBox.Show(
                this,
                "Добавьте хотя бы одно слово в словарь.",
                "Тренировка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        CreateNextTrainingQuestion();
    }

    private void CreateNextTrainingQuestion()
    {
        var mode = currentTrainingStatistics?.Mode ?? GetSelectedTrainingMode();

        currentQuestion = trainingSession.CreateQuestion(wordEntries, mode);
        trainingQuestionLabel.Text = currentQuestion.QuestionText;
        trainingAnswerTextBox.Clear();
        trainingResultLabel.Text = "";
        trainingResultLabel.ForeColor = mutedTextColor;
        checkAnswerButton.Enabled = true;
        nextQuestionButton.Enabled = true;
        trainingAnswerTextBox.Focus();
    }

    private TrainingMode GetSelectedTrainingMode()
    {
        return trainingModeComboBox.SelectedIndex == 1
            ? TrainingMode.TranslationToWord
            : TrainingMode.WordToTranslation;
    }

    private void SaveCurrentTrainingProgress()
    {
        if (currentTrainingStatistics is null || trainingSession.TotalQuestions == 0)
        {
            return;
        }

        currentTrainingStatistics.TotalQuestions = trainingSession.TotalQuestions;
        currentTrainingStatistics.CorrectAnswers = trainingSession.CorrectAnswers;
        currentTrainingStatistics.WrongAnswers = trainingSession.WrongAnswers;

        if (!trainingStatistics.Contains(currentTrainingStatistics))
        {
            trainingStatistics.Add(currentTrainingStatistics);
        }

        TrySaveTrainingStatistics();
        RefreshTrainingHistory();
    }

    private void RefreshTrainingHistory()
    {
        var orderedStatistics = trainingStatistics
            .OrderByDescending(item => item.StartedAt)
            .ToList();


        trainingStatisticsGrid.DataSource = orderedStatistics
            .Select(item => new TrainingHistoryRow
            {
                Date = item.StartedAt.ToString("dd.MM.yyyy HH:mm"),
                Mode = FormatTrainingMode(item.Mode),
                TotalQuestions = item.TotalQuestions,
                CorrectAnswers = item.CorrectAnswers,
                WrongAnswers = item.WrongAnswers,
                Accuracy = $"{item.AccuracyPercent:0}%"
            })
            .ToList();

        var totalQuestions = orderedStatistics.Sum(item => item.TotalQuestions);
        var totalCorrectAnswers = orderedStatistics.Sum(item => item.CorrectAnswers);
        var totalAccuracy = totalQuestions == 0
            ? 0
            : totalCorrectAnswers * 100.0 / totalQuestions;

        totalTrainingsHistoryLabel.Text = orderedStatistics.Count.ToString();
        totalQuestionsHistoryLabel.Text = totalQuestions.ToString();
        correctAnswersHistoryLabel.Text = totalCorrectAnswers.ToString();
        averageAccuracyHistoryLabel.Text = $"{totalAccuracy:0}%";
    }

    private static string FormatTrainingMode(TrainingMode mode)
    {
        return mode == TrainingMode.WordToTranslation
            ? "Слово → перевод"
            : "Перевод → слово";
    }
    private void UpdateTrainingStats()
    {
        var percent = trainingSession.TotalQuestions == 0
            ? 0
            : trainingSession.CorrectAnswers * 100.0 / trainingSession.TotalQuestions;

        trainingStatsLabel.Text =
            $"Всего вопросов: {trainingSession.TotalQuestions}{Environment.NewLine}{Environment.NewLine}" +
            $"Правильных ответов: {trainingSession.CorrectAnswers}{Environment.NewLine}{Environment.NewLine}" +
            $"Ошибок: {trainingSession.WrongAnswers}{Environment.NewLine}{Environment.NewLine}" +
            $"Процент правильных: {percent:0}%";
    }

    private void ClearWordInputFields()
    {
        wordTextBox.Clear();
        translationTextBox.Clear();
        categoryTextBox.Clear();
    }

    private void ShowHelp()
    {
        const string helpText =
            "Горячие клавиши:\n" +
            "Ctrl + N — очистить поля нового слова\n" +
            "Ctrl + F — перейти в поиск\n" +
            "Ctrl + S — экспортировать CSV\n" +
            "F1 — показать эту справку\n\n" +
            "Как добавлять слова:\n" +
            "Поле «Слово» всегда содержит русский вариант, а «Перевод» — английский. Если заполнить их наоборот, программа автоматически поменяет значения местами. Все изменения автоматически сохраняются в words.json рядом с программой.\n\n" +
            "Автоперевод:\n" +
            "Введите русское или английское слово и нажмите «Автоперевод». После перевода поле «Слово» будет содержать русский вариант, а «Перевод» — английский.\n\n" +
            "Импорт/экспорт CSV:\n" +
            "Откройте вкладку «Импорт/экспорт». Без флага загружается готовый CSV с колонками Word, Translation, Category. Для списка русских или английских слов включите флаг «Автоперевод одной колонки»; имя файла станет категорией.\n\n" +
            "Тренировка:\n" +
            "Откройте вкладку «Тренировка», выберите режим, нажмите «Начать тренировку», введите ответ и нажмите «Проверить». После каждого ответа история сохраняется в training-statistics.json и отображается на вкладке «Статистика».";

        MessageBox.Show(this, helpText, "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed class TrainingHistoryRow
    {
        public string Date { get; init; } = string.Empty;

        public string Mode { get; init; } = string.Empty;

        public int TotalQuestions { get; init; }

        public int CorrectAnswers { get; init; }

        public int WrongAnswers { get; init; }

        public string Accuracy { get; init; } = string.Empty;
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.N))
        {
            ClearWordInputFields();
            mainTabControl.SelectedTab = dictionaryTabPage;
            wordTextBox.Focus();
            UpdateStatus("Поля очищены.");
            return true;
        }

        if (keyData == (Keys.Control | Keys.F))
        {
            mainTabControl.SelectedTab = dictionaryTabPage;
            searchTextBox.Focus();
            searchTextBox.SelectAll();
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            ExportToCsv();
            return true;
        }

        if (keyData == Keys.F1)
        {
            ShowHelp();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
