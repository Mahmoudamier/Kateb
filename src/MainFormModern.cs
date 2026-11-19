using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ImageTextMaker
{
    public class MainForm : Form
    {
        private enum LogKind { Idle, Info, Ok, Run, Skip, Error }

        private readonly List<BatchCard> _cards = new List<BatchCard>();

        private ModeTab _tabSingle;
        private ModeTab _tabBatch;
        private Panel _singlePanel;
        private Panel _batchPanel;
        private TableLayoutPanel _rootLayout;
        private ScrollHost _batchList;
        private bool _singleMode = true;

        private DropZone _singleDrop;
        private DropZone _batchDrop;
        private HintTextBox _singlePath;
        private HintTextBox _batchPath;
        private HintTextBox _singleText;
        private HintTextBox _singleTitle;
        private HintTextBox _singleFolder;
        private HintTextBox _batchFolder;

        private RoundedButton _singleGenerate;
        private RoundedButton _batchGenerate;
        private RoundedButton _clearAction;
        private NumericUpDown _fontSize;
        private CheckBox _chkAuto;
        private CheckBox _chkPanel;
        private CheckBox _chkOverwrite;

        private Label _singleCount;
        private Label _batchCount;
        private PillLabel _status;
        private ProgressPanel _progress;
        private RichTextBox _log;
        private Panel _logBody;
        private RoundedButton _logToggle;
        private bool _logCollapsed;

        private Image _logoImage;
        private bool _busy;

        public MainForm()
        {
            Text = "كاتِب — Kateb";
            Size = new Size(1280, 860);
            MinimumSize = new Size(1040, 720);
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            Font = UiTheme.F(11f, false);
            BackColor = UiTheme.Bg;
            ForeColor = UiTheme.Ink;

            BuildUi();
            FixAutoSize(this);
            ShowMode(true);
            UpdateBatchSummary();
            SetStatus("جاهز", PillLabel.Kind.Idle);
            _progress.Value = 0;
            Log("مستني أول عملية...", LogKind.Info);
        }

        private void BuildUi()
        {
            _rootLayout = new TableLayoutPanel();
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.ColumnCount = 1;
            _rootLayout.RowCount = 6;
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _rootLayout.Padding = new Padding(14, 12, 14, 12);
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));  // header + gap
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // modes + gap
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94f));  // settings + gap
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // workspace
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));  // actions + gap
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132f)); // log

            _rootLayout.Controls.Add(BuildHeader(), 0, 0);
            _rootLayout.Controls.Add(BuildTabs(), 0, 1);
            _rootLayout.Controls.Add(BuildSettings(), 0, 2);

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Margin = new Padding(0, 0, 0, 10);
            _singlePanel = BuildSinglePanel();
            _batchPanel = BuildBatchPanel();
            host.Controls.Add(_batchPanel);
            host.Controls.Add(_singlePanel);
            _rootLayout.Controls.Add(host, 0, 3);

            _rootLayout.Controls.Add(BuildActionBar(), 0, 4);
            _rootLayout.Controls.Add(BuildLog(), 0, 5);
            Controls.Add(_rootLayout);
        }

        // -----------------------------------------------------------------
        // Header — logo and name grouped, version as secondary information
        // -----------------------------------------------------------------
        private CardPanel BuildHeader()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 0, 10);
            card.Padding = new Padding(10, 4, 10, 4);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 2;
            t.RowCount = 1;
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Logo and name live together at the right edge (RTL start).
            TableLayoutPanel brand = new TableLayoutPanel();
            brand.Dock = DockStyle.Fill;
            brand.Margin = Padding.Empty;
            brand.ColumnCount = 2;
            brand.RowCount = 2;
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44f));
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 25f));
            brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            PictureBox logo = new PictureBox();
            logo.Dock = DockStyle.Fill;
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Margin = new Padding(0, 2, 10, 2);
            try
            {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "logo.png");
                if (File.Exists(p))
                {
                    _logoImage = Image.FromFile(p);
                    logo.Image = _logoImage;
                }
            }
            catch { }
            brand.Controls.Add(logo, 0, 0);
            brand.SetRowSpan(logo, 2);

            Label title = new Label();
            title.Text = "كاتِب";
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.F(16, true);
            title.ForeColor = UiTheme.Ink;
            title.TextAlign = ContentAlignment.MiddleRight;
            brand.Controls.Add(title, 1, 0);

            Label sub = new Label();
            sub.Text = "نص عربي في نص الصورة — دقيق وسريع";
            sub.Dock = DockStyle.Fill;
            sub.Font = UiTheme.F(9.5f, false);
            sub.ForeColor = UiTheme.Muted;
            sub.TextAlign = ContentAlignment.MiddleRight;
            brand.Controls.Add(sub, 1, 1);
            t.Controls.Add(brand, 0, 0);

            // Version stays as quiet secondary information, not a coloured badge.
            Label version = new Label();
            version.Text = "النسخة 2.0 · شغال من غير إنترنت";
            version.Dock = DockStyle.Fill;
            version.Font = UiTheme.F(9.5f, false);
            version.ForeColor = UiTheme.Muted;
            version.TextAlign = ContentAlignment.MiddleLeft;
            version.Margin = new Padding(12, 0, 0, 0);
            t.Controls.Add(version, 1, 0);

            card.Controls.Add(t);
            return card;
        }

        // -----------------------------------------------------------------
        // Segmented working-mode selector
        // -----------------------------------------------------------------
        private CardPanel BuildTabs()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 0, 8);
            card.Padding = new Padding(5);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 2;
            t.RowCount = 1;
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // A compact segmented track instead of two full-width empty pills.
            SoftPanel track = new SoftPanel();
            track.Dock = DockStyle.Fill;
            track.Margin = Padding.Empty;
            track.Padding = new Padding(3);

            TableLayoutPanel seg = new TableLayoutPanel();
            seg.Dock = DockStyle.Fill;
            seg.ColumnCount = 2;
            seg.RowCount = 1;
            seg.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            seg.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            seg.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _tabSingle = new ModeTab("صورة واحدة");
            _tabSingle.Dock = DockStyle.Fill;
            _tabSingle.Active = true;
            _tabSingle.Click += delegate { ShowMode(true); };

            _tabBatch = new ModeTab("10 صور في مرة واحدة");
            _tabBatch.Dock = DockStyle.Fill;
            _tabBatch.Active = false;
            _tabBatch.Click += delegate { ShowMode(false); };

            seg.Controls.Add(_tabSingle, 0, 0);
            seg.Controls.Add(_tabBatch, 1, 0);
            track.Controls.Add(seg);
            t.Controls.Add(track, 0, 0);

            Label hint = new Label();
            hint.Text = "تقدر تبدّل بين الوضعين في أي وقت من غير ما تخسر اللي كتبته";
            hint.Dock = DockStyle.Fill;
            hint.Font = UiTheme.F(9.5f, false);
            hint.ForeColor = UiTheme.Muted;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.Margin = new Padding(14, 0, 4, 0);
            t.Controls.Add(hint, 1, 0);

            card.Controls.Add(t);
            return card;
        }

        // -----------------------------------------------------------------
        // Text settings
        // -----------------------------------------------------------------
        private CardPanel BuildSettings()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 0, 10);
            card.Padding = new Padding(12, 4, 12, 4);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel head = new TableLayoutPanel();
            head.Dock = DockStyle.Fill;
            head.Margin = Padding.Empty;
            head.ColumnCount = 2;
            head.RowCount = 1;
            head.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330f));

            Label title = new Label();
            title.Text = "إعدادات النص";
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.F(13, true);
            title.ForeColor = UiTheme.Ink;
            title.TextAlign = ContentAlignment.MiddleRight;
            Label hint = new Label();
            hint.Text = "شغالة في الوضعين — النص بيطلع زي ما كتبته بالظبط";
            hint.Dock = DockStyle.Fill;
            hint.Font = UiTheme.F(9.5f, false);
            hint.ForeColor = UiTheme.Muted;
            hint.TextAlign = ContentAlignment.MiddleRight;
            head.Controls.Add(title, 0, 0);
            head.Controls.Add(hint, 1, 0);
            t.Controls.Add(head, 0, 0);

            // One compact row: font size, a divider, then the three options.
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.Margin = Padding.Empty;
            row.ColumnCount = 6;
            row.RowCount = 1;
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label fontLabel = new Label();
            fontLabel.Text = "حجم الخط (بكسل)";
            fontLabel.Dock = DockStyle.Fill;
            fontLabel.Font = UiTheme.F(10.5f, true);
            fontLabel.ForeColor = UiTheme.Ink;
            fontLabel.TextAlign = ContentAlignment.MiddleRight;
            fontLabel.Margin = new Padding(0, 0, 8, 0);
            row.Controls.Add(fontLabel, 0, 0);

            _fontSize = new NumericUpDown();
            _fontSize.Minimum = 12;
            _fontSize.Maximum = 160;
            _fontSize.Value = 30;
            _fontSize.Dock = DockStyle.Fill;
            _fontSize.Font = UiTheme.F(11, false);
            _fontSize.RightToLeft = RightToLeft.Yes;
            _fontSize.Margin = new Padding(0, 2, 0, 2);
            row.Controls.Add(_fontSize, 1, 0);

            Panel divider = new Panel();
            divider.Dock = DockStyle.Fill;
            divider.Margin = new Padding(10, 5, 10, 5);
            divider.BackColor = UiTheme.Line;
            row.Controls.Add(divider, 2, 0);

            _chkAuto = NewCheck("ضبط تلقائي على مقاس الصورة", true);
            _chkPanel = NewCheck("خلفية داكنة ورا النص", true);
            _chkOverwrite = NewCheck("بدّل الملفات الموجودة (الدفعة)", true);
            row.Controls.Add(_chkAuto, 3, 0);
            row.Controls.Add(_chkPanel, 4, 0);
            row.Controls.Add(_chkOverwrite, 5, 0);
            t.Controls.Add(row, 0, 1);

            card.Controls.Add(t);
            return card;
        }

        private static CheckBox NewCheck(string text, bool value)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Checked = value;
            c.Dock = DockStyle.Fill;
            c.RightToLeft = RightToLeft.Yes;
            c.CheckAlign = ContentAlignment.MiddleRight;
            c.TextAlign = ContentAlignment.MiddleRight;
            c.Margin = new Padding(8, 5, 8, 5);
            c.Font = UiTheme.F(10.5f, false);
            c.ForeColor = UiTheme.Ink;
            return c;
        }

        // -----------------------------------------------------------------
        // Single-image workspace
        // -----------------------------------------------------------------
        private Panel BuildSinglePanel()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = Padding.Empty;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel imageArea = BuildImageArea(false);
            root.Controls.Add(imageArea, 0, 0);

            ScrollHost scroller = new ScrollHost();
            scroller.Dock = DockStyle.Fill;
            TableLayoutPanel c = scroller.Content;
            c.RowCount = 4;
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 104f));
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

            _singleCount = new Label();
            _singleCount.Text = "0 حرف";
            c.Controls.Add(SectionHead("النص والعنوان", _singleCount), 0, 0);

            _singleText = new HintTextBox();
            _singleText.Dock = DockStyle.Fill;
            _singleText.Multiline = true;
            _singleText.AcceptsReturn = true;
            _singleText.ScrollBars = ScrollBars.Vertical;
            _singleText.Hint = "اكتب النص هنا زي ما عايزه يظهر جوّه الصورة...";
            _singleText.Font = UiTheme.F(11, false);
            _singleText.Margin = new Padding(6, 2, 6, 2);
            _singleText.TextChanged += delegate
            {
                _singleCount.Text = _singleText.Text.Length + " حرف";
                UpdateGenerateStates();
            };
            c.Controls.Add(_singleText, 0, 1);

            _singleTitle = new HintTextBox();
            _singleTitle.Dock = DockStyle.Fill;
            _singleTitle.Hint = "اسم الملف من غير .png — سيبه فاضي وهنسمّيه تلقائي";
            _singleTitle.Font = UiTheme.F(10.5f, false);
            _singleTitle.Margin = new Padding(6, 2, 6, 2);
            c.Controls.Add(_singleTitle, 0, 2);

            Label tip = new Label();
            tip.Text = "النص بيظهر جوّه الصورة زي ما كتبته بالظبط — من غير تعديل أو ترتيب.";
            tip.Dock = DockStyle.Fill;
            tip.Font = UiTheme.F(9.5f, false);
            tip.ForeColor = UiTheme.Muted;
            tip.TextAlign = ContentAlignment.MiddleRight;
            tip.Margin = new Padding(8, 0, 8, 0);
            c.Controls.Add(tip, 0, 3);
            scroller.FitContent();

            root.Controls.Add(scroller, 0, 1);
            card.Controls.Add(root);
            return card;
        }

        // -----------------------------------------------------------------
        // Batch workspace — one single scrolling region for all 10 cards
        // -----------------------------------------------------------------
        private Panel BuildBatchPanel()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = Padding.Empty;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildImageArea(true), 0, 0);

            _batchCount = new Label();
            _batchCount.Text = "0 / 10 خلصت";
            root.Controls.Add(SectionHead("الصور من 1 لـ 10 — لكل صورة عنوان ونص", _batchCount), 0, 1);

            _batchList = new ScrollHost();
            _batchList.Dock = DockStyle.Fill;
            TableLayoutPanel list = _batchList.Content;
            list.RowCount = 10;
            for (int i = 0; i < 10; i++)
            {
                list.RowStyles.Add(new RowStyle(SizeType.Absolute, 104f));
                BatchCard item = new BatchCard(i + 1);
                item.Dock = DockStyle.Fill;
                item.Margin = new Padding(0, 0, 0, 8);
                item.ContentChanged += delegate { UpdateBatchSummary(); };
                item.CopyRequested += delegate(object s, EventArgs e) { CopyCard((BatchCard)s); };
                item.ClearRequested += delegate(object s, EventArgs e)
                {
                    BatchCard bc = (BatchCard)s;
                    bc.Clear();
                    UpdateBatchSummary();
                    Log("اتمسحت الصورة " + bc.Number + ".", LogKind.Info);
                };
                item.HeightChanged += delegate(object s, EventArgs e)
                {
                    BatchCard bc = (BatchCard)s;
                    int target = bc.Collapsed ? BatchCard.CollapsedHeight : BatchCard.ExpandedHeight;
                    _batchList.SetRowHeight(bc.Number - 1, target + 8);
                };
                _cards.Add(item);
                list.Controls.Add(item, 0, i);
            }
            _batchList.FitContent();
            root.Controls.Add(_batchList, 0, 2);

            card.Controls.Add(root);
            return card;
        }

        private Panel BuildImageArea(bool batch)
        {
            Panel area = new Panel();
            area.Dock = DockStyle.Fill;
            area.Margin = new Padding(0, 0, 0, 10);
            area.BackColor = UiTheme.Soft;

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 2;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label head = new Label();
            head.Text = batch
                ? "الصورة الثابتة — نفس الصورة لكل النصوص، تختارها مرة"
                : "الصورة — اختار الصورة اللي النص هيكتب عليها";
            head.Dock = DockStyle.Fill;
            head.Font = UiTheme.F(10.5f, true);
            head.ForeColor = UiTheme.Ink;
            head.TextAlign = ContentAlignment.MiddleRight;
            t.Controls.Add(head, 0, 0);
            t.SetColumnSpan(head, 2);

            DropZone drop = new DropZone("أو اضغط «اختار صورة»");
            drop.Dock = DockStyle.Fill;
            drop.Margin = new Padding(8, 4, 8, 4);
            drop.Click += delegate { PickImage(!batch); };
            drop.DragEnter += ImageDragEnter;
            drop.DragDrop += delegate(object s, DragEventArgs e) { DropImage(e, !batch); };

            HintTextBox path = new HintTextBox();
            path.Dock = DockStyle.Fill;
            path.ReadOnly = true;
            path.BackColor = UiTheme.Card;
            path.Hint = "ما اختارتش صورة لسه";
            path.Font = UiTheme.F(9.5f, false);
            path.RightToLeft = RightToLeft.No;
            path.TextAlign = HorizontalAlignment.Left;
            path.Margin = new Padding(0, 1, 0, 1);
            path.TextChanged += delegate { UpdateGenerateStates(); };

            RoundedButton pick = new RoundedButton();
            pick.Text = "اختار صورة";
            pick.ApplySoft();
            pick.Dock = DockStyle.Fill;
            pick.Margin = new Padding(0, 1, 0, 1);
            pick.Click += delegate { PickImage(!batch); };

            TableLayoutPanel side = new TableLayoutPanel();
            side.Dock = DockStyle.Fill;
            side.ColumnCount = 1;
            side.RowCount = 2;
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            side.Controls.Add(path, 0, 0);
            side.Controls.Add(pick, 0, 1);

            t.Controls.Add(drop, 0, 1);
            t.Controls.Add(side, 1, 1);
            area.Controls.Add(t);

            if (batch) { _batchDrop = drop; _batchPath = path; }
            else { _singleDrop = drop; _singlePath = path; }
            return area;
        }

        // -----------------------------------------------------------------
        // Shared action bar: output folder + generate
        // -----------------------------------------------------------------
        private CardPanel BuildActionBar()
        {
            CardPanel bar = new CardPanel();
            bar.Dock = DockStyle.Fill;
            bar.Margin = new Padding(0, 0, 0, 10);
            bar.Padding = new Padding(10, 4, 10, 4);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));

            TableLayoutPanel folderRow = new TableLayoutPanel();
            folderRow.Dock = DockStyle.Fill;
            folderRow.ColumnCount = 3;
            folderRow.RowCount = 1;
            folderRow.Margin = new Padding(0, 0, 0, 4);
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            folderRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label folderLabel = new Label();
            folderLabel.Text = "مجلد الحفظ";
            folderLabel.Dock = DockStyle.Fill;
            folderLabel.Font = UiTheme.F(10.5f, true);
            folderLabel.ForeColor = UiTheme.Ink;
            folderLabel.TextAlign = ContentAlignment.MiddleRight;

            Panel folderHost = new Panel();
            folderHost.Dock = DockStyle.Fill;
            folderHost.Margin = new Padding(4, 2, 4, 2);
            _singleFolder = NewFolderBox();
            _batchFolder = NewFolderBox();
            _batchFolder.Visible = false;
            folderHost.Controls.Add(_batchFolder);
            folderHost.Controls.Add(_singleFolder);

            RoundedButton browse = new RoundedButton();
            browse.Text = "تصفّح";
            browse.ApplySoft();
            browse.Dock = DockStyle.Fill;
            browse.Margin = new Padding(4, 2, 0, 2);
            browse.Click += delegate
            {
                if (_singleMode) PickFolder(_singleFolder); else PickFolder(_batchFolder);
            };

            folderRow.Controls.Add(folderLabel, 0, 0);
            folderRow.Controls.Add(folderHost, 1, 0);
            folderRow.Controls.Add(browse, 2, 0);
            t.Controls.Add(folderRow, 0, 0);

            TableLayoutPanel generateRow = new TableLayoutPanel();
            generateRow.Dock = DockStyle.Fill;
            generateRow.ColumnCount = 2;
            generateRow.RowCount = 1;
            generateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            generateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
            generateRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel generateHost = new Panel();
            generateHost.Dock = DockStyle.Fill;
            generateHost.Margin = new Padding(0, 2, 4, 2);
            _singleGenerate = NewGenerate("اعمل الصورة");
            _batchGenerate = NewGenerate("اعمل الصور");
            _batchGenerate.Visible = false;
            generateHost.Controls.Add(_batchGenerate);
            generateHost.Controls.Add(_singleGenerate);

            _clearAction = new RoundedButton();
            _clearAction.Text = "مسح";
            _clearAction.ApplyGhost();
            _clearAction.Dock = DockStyle.Fill;
            _clearAction.Margin = new Padding(4, 2, 0, 2);
            _clearAction.Click += delegate
            {
                if (_singleMode)
                {
                    _singleText.Text = "";
                    _singleTitle.Text = "";
                    Log("اتمسح نص وعنوان الصورة الواحدة.", LogKind.Info);
                    UpdateGenerateStates();
                }
                else ClearAll_Click(this, EventArgs.Empty);
            };

            generateRow.Controls.Add(generateHost, 0, 0);
            generateRow.Controls.Add(_clearAction, 1, 0);
            t.Controls.Add(generateRow, 0, 1);

            _singleGenerate.Click += SingleGenerate_Click;
            _batchGenerate.Click += BatchGenerate_Click;

            bar.Controls.Add(t);
            return bar;
        }

        private static HintTextBox NewFolderBox()
        {
            HintTextBox box = new HintTextBox();
            box.Dock = DockStyle.Fill;
            box.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            box.Font = UiTheme.F(10, false);
            box.RightToLeft = RightToLeft.No;
            box.TextAlign = HorizontalAlignment.Left;
            box.Margin = Padding.Empty;
            box.Hint = "مسار مجلد الحفظ";
            return box;
        }

        private static RoundedButton NewGenerate(string text)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.ApplyPrimary();
            b.Dock = DockStyle.Fill;
            b.LoadingText = "شغال...";
            return b;
        }

        // -----------------------------------------------------------------
        // Execution log
        // -----------------------------------------------------------------
        private Panel BuildLog()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(10, 4, 10, 4);
            card.Margin = Padding.Empty;

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 3;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 8f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel head = new TableLayoutPanel();
            head.Dock = DockStyle.Fill;
            head.Margin = Padding.Empty;
            head.ColumnCount = 4;
            head.RowCount = 1;
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28f));
            head.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label title = new Label();
            title.Text = "سجل العمليات";
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.F(11.5f, true);
            title.ForeColor = UiTheme.Ink;
            title.TextAlign = ContentAlignment.MiddleRight;

            _status = new PillLabel();
            _status.Dock = DockStyle.Fill;
            _status.Margin = new Padding(4, 2, 4, 2);

            RoundedButton clear = new RoundedButton();
            clear.Text = "امسح السجل";
            clear.ApplyGhost();
            clear.Dock = DockStyle.Fill;
            clear.Margin = new Padding(4, 2, 2, 2);
            clear.Click += delegate
            {
                _log.Clear();
                Log("اتمسح السجل.", LogKind.Info);
            };

            _logToggle = new RoundedButton();
            _logToggle.Text = "▾";
            _logToggle.Font = new Font("Segoe UI", 9f);
            _logToggle.ApplyGhost();
            _logToggle.Dock = DockStyle.Fill;
            _logToggle.Margin = new Padding(2, 2, 0, 2);
            _logToggle.Click += ToggleLog;

            head.Controls.Add(title, 0, 0);
            head.Controls.Add(_status, 1, 0);
            head.Controls.Add(clear, 2, 0);
            head.Controls.Add(_logToggle, 3, 0);
            t.Controls.Add(head, 0, 0);

            _progress = new ProgressPanel();
            _progress.Dock = DockStyle.Fill;
            _progress.Margin = new Padding(4, 0, 4, 0);
            t.Controls.Add(_progress, 0, 1);

            _logBody = new Panel();
            _logBody.Dock = DockStyle.Fill;
            _log = new RichTextBox();
            _log.Dock = DockStyle.Fill;
            _log.ReadOnly = true;
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = UiTheme.Soft;
            _log.Font = UiTheme.F(10, false);
            _log.RightToLeft = RightToLeft.Yes;
            _log.ScrollBars = RichTextBoxScrollBars.Vertical;
            _logBody.Controls.Add(_log);
            t.Controls.Add(_logBody, 0, 2);

            card.Controls.Add(t);
            return card;
        }

        private void ToggleLog(object sender, EventArgs e)
        {
            SetLogCollapsed(!_logCollapsed);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------
        private static void FixAutoSize(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label || c is CheckBox) c.AutoSize = false;
                FixAutoSize(c);
            }
        }

        private static Panel SectionHead(string text, Label counter)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 2;
            t.RowCount = 1;
            t.Margin = new Padding(4, 0, 4, 0);
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label l = new Label();
            l.Text = text;
            l.Dock = DockStyle.Fill;
            l.Font = UiTheme.F(11, true);
            l.ForeColor = UiTheme.Ink;
            l.TextAlign = ContentAlignment.MiddleRight;
            t.Controls.Add(l, 0, 0);

            if (counter != null)
            {
                counter.Dock = DockStyle.Fill;
                counter.Font = UiTheme.F(10, true);
                counter.ForeColor = UiTheme.Muted;
                counter.TextAlign = ContentAlignment.MiddleLeft;
                t.Controls.Add(counter, 1, 0);
            }
            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Margin = Padding.Empty;
            host.Controls.Add(t);
            return host;
        }

        private void ShowMode(bool single)
        {
            _singleMode = single;
            _tabSingle.Active = single;
            _tabBatch.Active = !single;
            _singlePanel.Visible = single;
            _batchPanel.Visible = !single;
            _singleFolder.Visible = single;
            _batchFolder.Visible = !single;
            _singleGenerate.Visible = single;
            _batchGenerate.Visible = !single;
            if (single)
            {
                _singlePanel.BringToFront();
                _singleFolder.BringToFront();
                _singleGenerate.BringToFront();
            }
            else
            {
                _batchPanel.BringToFront();
                _batchFolder.BringToFront();
                _batchGenerate.BringToFront();
            }
            _clearAction.Text = single ? "مسح" : "مسح الكل";
            UpdateGenerateStates();
        }

        private static void ImageDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void DropImage(DragEventArgs e, bool single)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0) SetImage(files[0], single);
        }

        private void PickImage(bool single)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "اختار الصورة";
            ofd.Filter = "ملفات الصور|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff";
            if (ofd.ShowDialog() == DialogResult.OK) SetImage(ofd.FileName, single);
        }

        private static void PickFolder(HintTextBox target)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "اختار مجلد الحفظ";
            if (fbd.ShowDialog() == DialogResult.OK) target.Text = fbd.SelectedPath;
        }

        private void SetImage(string path, bool single)
        {
            if (!File.Exists(path)) return;
            try
            {
                // The preview box is tiny. Keep only a small thumbnail instead of a
                // full-resolution copy (a 4000x3000 image would cost ~48 MB in memory
                // and make every repaint slower). Generation still reads the original
                // file from this path, so exported quality is unchanged.
                Bitmap preview;
                using (Image img = Image.FromFile(path))
                    preview = MakePreview(img);
                if (single) _singleDrop.Preview = preview; else _batchDrop.Preview = preview;
                if (single) _singlePath.Text = path; else _batchPath.Text = path;
                Log("اتحملت الصورة: " + Path.GetFileName(path), LogKind.Ok);
                UpdateGenerateStates();
            }
            catch (Exception ex)
            {
                Error("ما قدرتش أفتح الصورة: " + ex.Message);
            }
        }

        private static Bitmap MakePreview(Image source)
        {
            const int max = 720;
            int w = source.Width, h = source.Height;
            if (w <= max && h <= max) return new Bitmap(source);
            double scale = Math.Min((double)max / w, (double)max / h);
            int nw = Math.Max(1, (int)Math.Round(w * scale));
            int nh = Math.Max(1, (int)Math.Round(h * scale));
            Bitmap b = new Bitmap(nw, nh);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.DrawImage(source, new Rectangle(0, 0, nw, nh));
            }
            return b;
        }

        // -----------------------------------------------------------------
        // Generation — original processing logic, unchanged
        // -----------------------------------------------------------------
        private void SingleGenerate_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            StartBusy(_singleGenerate);
            try
            {
                string img = _singlePath.Text;
                if (!File.Exists(img)) { Warn("اختار الصورة الأول."); return; }
                if (_singleText.Text.Length == 0) { Warn("اكتب النص اللي عايزه الأول."); return; }

                string folder = ResolveFolder(_singleFolder.Text);
                string title = Sanitize(_singleTitle.Text.Trim());
                if (title.Length == 0) title = Sanitize(Path.GetFileNameWithoutExtension(img)) + "_مكتوب";
                string outPath = Path.Combine(folder, title + ".png");

                if (File.Exists(outPath))
                {
                    DialogResult ans = MessageBox.Show("الملف موجود أصلاً. عايز تبدّله؟", "تأكيد",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading);
                    if (ans != DialogResult.Yes) return;
                }

                SetStatus("شغال", PillLabel.Kind.Processing);
                _progress.Value = 35;
                Log("باعمل الصورة...", LogKind.Run);
                SaveOne(img, _singleText.Text, title, folder,
                    (float)_fontSize.Value, _chkAuto.Checked, _chkPanel.Checked);
                _progress.Value = 100;
                SetStatus("خلص", PillLabel.Kind.Done);
                Log("اتعمل: " + outPath, LogKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("خطأ", PillLabel.Kind.Error);
                Error(ex);
                Log("خطأ: " + ex.Message, LogKind.Error);
            }
            finally
            {
                EndBusy(_singleGenerate);
            }
        }

        private void BatchGenerate_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            StartBusy(_batchGenerate);
            try
            {
                string img = _batchPath.Text;
                if (!File.Exists(img)) { Warn("اختار الصورة الثابتة الأول."); return; }

                List<BatchCard> filled = new List<BatchCard>();
                for (int i = 0; i < _cards.Count; i++)
                    if (_cards[i].TextValue.Length > 0) filled.Add(_cards[i]);

                if (filled.Count == 0) { Warn("اكتب نص ولو في صورة واحدة."); return; }

                string folder = ResolveFolder(_batchFolder.Text);
                int done = 0, skipped = 0;

                SetStatus("شغال 0 من " + filled.Count, PillLabel.Kind.Processing);
                _progress.Value = 0;
                Log("بدأت أعمل " + filled.Count + " صورة...", LogKind.Run);
                Application.DoEvents();

                for (int i = 0; i < filled.Count; i++)
                {
                    BatchCard card = filled[i];
                    card.SetStatus(CardStatus.Processing);
                    string title = Sanitize(card.TitleValue.Trim());
                    if (title.Length == 0) title = "صورة_" + card.Number;
                    string outPath = Path.Combine(folder, title + ".png");

                    if (File.Exists(outPath) && !_chkOverwrite.Checked)
                    {
                        card.SetStatus(CardStatus.Error);
                        Log("اتخطى: " + title + " (الملف موجود)", LogKind.Skip);
                        skipped++;
                    }
                    else
                    {
                        try
                        {
                            SaveOne(img, card.TextValue, title, folder,
                                (float)_fontSize.Value, _chkAuto.Checked, _chkPanel.Checked);
                            card.SetStatus(CardStatus.Done);
                            Log("خلص: " + title + ".png", LogKind.Ok);
                            done++;
                        }
                        catch (Exception ex)
                        {
                            card.SetStatus(CardStatus.Error);
                            Log("خطأ في " + title + ": " + ex.Message, LogKind.Error);
                            skipped++;
                        }
                    }

                    SetStatus("شغال " + (i + 1) + " من " + filled.Count, PillLabel.Kind.Processing);
                    _progress.Value = ((i + 1) * 100) / filled.Count;
                    Application.DoEvents();
                }

                SetStatus("خلص", PillLabel.Kind.Done);
                Log(" الخلاصة: " + done + " صورة خلصت، " + skipped + " اتخطت.", LogKind.Ok);
            }
            catch (Exception ex)
            {
                SetStatus("خطأ", PillLabel.Kind.Error);
                Error(ex);
                Log("خطأ: " + ex.Message, LogKind.Error);
            }
            finally
            {
                EndBusy(_batchGenerate);
            }
        }

        private void StartBusy(RoundedButton button)
        {
            _busy = true;
            button.Loading = true;
            _singleGenerate.Enabled = false;
            _batchGenerate.Enabled = false;
        }

        private void EndBusy(RoundedButton button)
        {
            button.Loading = false;
            _busy = false;
            UpdateGenerateStates();
        }

        private void ClearAll_Click(object sender, EventArgs e)
        {
            bool any = false;
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].TitleValue.Length > 0 || _cards[i].TextValue.Length > 0) { any = true; break; }
            if (any)
            {
                DialogResult ans = MessageBox.Show("هتمسح كل العناوين والنصوص. متأكد؟", "تأكيد",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading);
                if (ans != DialogResult.Yes) return;
            }
            for (int i = 0; i < _cards.Count; i++) _cards[i].Clear();
            UpdateBatchSummary();
            Log("اتمسحت كل الخانات.", LogKind.Info);
        }

        private void CopyCard(BatchCard card)
        {
            if (card.TextValue.Length == 0) { Log("مفيش نص في الصورة " + card.Number + " عشان يتنسخ.", LogKind.Skip); return; }
            try
            {
                Clipboard.SetText(card.TextValue);
                Log("اتنسخ نص الصورة " + card.Number + ".", LogKind.Ok);
            }
            catch (Exception ex)
            {
                Log("النسخ فشل: " + ex.Message, LogKind.Error);
            }
        }

        private void UpdateBatchSummary()
        {
            int n = 0;
            for (int i = 0; i < _cards.Count; i++) if (_cards[i].TextValue.Length > 0) n++;
            if (_batchCount != null) _batchCount.Text = n + " / 10 خلصت";
            UpdateGenerateStates();
        }

        private void UpdateGenerateStates()
        {
            if (_singleGenerate == null || _batchGenerate == null) return;
            if (_busy)
            {
                _singleGenerate.Enabled = false;
                _batchGenerate.Enabled = false;
                return;
            }
            _singleGenerate.Enabled = File.Exists(_singlePath.Text) && _singleText.Text.Length > 0;
            int n = 0;
            for (int i = 0; i < _cards.Count; i++) if (_cards[i].TextValue.Length > 0) n++;
            _batchGenerate.Enabled = File.Exists(_batchPath.Text) && n > 0;
        }

        private static void SaveOne(string img, string text, string title, string folder,
            float fontSize, bool auto, bool panel)
        {
            using (Bitmap bmp = new Bitmap(img))
            {
                ImageTextRenderer.Render(bmp, text, fontSize, auto, panel);
                bmp.Save(Path.Combine(folder, title + ".png"), ImageFormat.Png);
            }
        }

        private static string ResolveFolder(string folder)
        {
            string f = (folder ?? "").Trim();
            if (f.Length == 0) f = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!Directory.Exists(f)) Directory.CreateDirectory(f);
            return f;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            StringBuilder sb = new StringBuilder();
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char ch in s)
            {
                if (char.IsControl(ch) || Array.IndexOf(invalid, ch) >= 0) sb.Append('_');
                else sb.Append(ch);
            }
            return sb.ToString().Trim();
        }

        private void SetStatus(string text, PillLabel.Kind kind)
        {
            _status.Text = text;
            _status.SetKind(kind);
        }

        private void Log(string message, LogKind kind)
        {
            if (_log == null) return;
            string prefix;
            Color color;
            switch (kind)
            {
                case LogKind.Ok: prefix = "✔  "; color = UiTheme.Ok; break;
                case LogKind.Run: prefix = "◌  "; color = UiTheme.Info; break;
                case LogKind.Info: prefix = "ℹ  "; color = UiTheme.Info; break;
                case LogKind.Skip: prefix = "•  "; color = UiTheme.Warn; break;
                case LogKind.Error: prefix = "✖  "; color = UiTheme.Danger; break;
                default: prefix = "•  "; color = UiTheme.Muted; break;
            }
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = UiTheme.Muted;
            _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] ");
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = color;
            _log.AppendText(prefix + message + Environment.NewLine);
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }

        private static void Warn(string msg)
        {
            MessageBox.Show(msg, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
        }

        private static void Info(string msg)
        {
            MessageBox.Show(msg, "خلص", MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
        }

        private static void Error(string msg)
        {
            MessageBox.Show("حصل خطأ:\n" + msg, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
        }

        private static void Error(Exception ex)
        {
            Error(ex.Message);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_rootLayout == null || _logBody == null || _logToggle == null) return;
            // Give the image list more room on smaller windows.
            bool collapse = ClientSize.Height < 820;
            if (collapse == _logCollapsed) return;
            SetLogCollapsed(collapse);
        }

        private void SetLogCollapsed(bool collapsed)
        {
            _logCollapsed = collapsed;
            _logBody.Visible = !collapsed;
            _logToggle.Text = collapsed ? "▴" : "▾";
            _rootLayout.RowStyles[5].Height = collapsed ? 44f : 132f;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_logoImage != null) _logoImage.Dispose();
            base.OnFormClosed(e);
        }
    }
}
