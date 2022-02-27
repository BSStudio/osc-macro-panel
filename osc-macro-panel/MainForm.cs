using Bespoke.Osc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace osc_macro_panel
{

    public partial class MainForm : Form
    {

        private string configPath;

        public MainForm(string configPath)
        {
            this.configPath = configPath;
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                loadConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Configuration error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            CenterToScreen();
        }

        private const string KEYWORD_IP = "ip";
        private const string KEYWORD_TITLE = "title";
        private const string KEYWORD_SIZE = "size";
        private const string KEYWORD_MARGIN = "margin";
        private const string KEYWORD_FONT = "font";
        private const string KEYWORD_BUTTON = "button";
        private const string KEYWORD_EMPTY = "empty";
        private const string KEYWORD_NEWLINE = "newline";

        private int buttonWidth = 120;
        private int buttonHeight = 120;
        private int buttonMargin = 10;
        private const string BUTTON_FONT_NAME = "Montserrat";
        private string buttonFontName = BUTTON_FONT_NAME;
        private const int BUTTON_FONT_SIZE = 14;
        private int buttonFontSize = BUTTON_FONT_SIZE;
        private bool buttonFontBold = true;
        private bool buttonFontItalic = false;
        private Font buttonFont = new Font(BUTTON_FONT_NAME, BUTTON_FONT_SIZE, FontStyle.Bold);

        private void loadConfig()
        {
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(configPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't read configuration file '{configPath}':\r\n{ex.Message}");
            }
            int lineNr = 1;
            int currentRow = 0;
            int currentColumn = 0;
            foreach (string line in lines)
            {
                string[] linePieces = line.Split(':');
                switch (linePieces[0])
                {
                    case KEYWORD_IP:
                        if (linePieces.Length != 3)
                            throw new ConfigException(lineNr, "Row with keyword 'ip' should contain 2 data columns.");
                        IPAddress ip;
                        try
                        {
                            ip = IPAddress.Parse(linePieces[1]);
                        }
                        catch
                        {
                            throw new ConfigException(lineNr, "Invalid IP address.");
                        }
                        if (!int.TryParse(linePieces[2], out int port) || (port < 1) || (port > 65535))
                            throw new ConfigException(lineNr, "Invalid port.");
                        destinationEndPoint = new IPEndPoint(ip, port);
                        break;
                    case KEYWORD_TITLE:
                        if (linePieces.Length != 2)
                            throw new ConfigException(lineNr, "Row with keyword 'title' should contain 1 data column.");
                        Text = linePieces[1];
                        break;
                    case KEYWORD_SIZE:
                        if (linePieces.Length != 3)
                            throw new ConfigException(lineNr, "Row with keyword 'size' should contain 2 data columns.");
                        if (!int.TryParse(linePieces[1], out buttonWidth) || (buttonWidth < 1) || (buttonWidth > 500))
                            throw new ConfigException(lineNr, "Invalid width.");
                        if (!int.TryParse(linePieces[2], out buttonHeight) || (buttonHeight < 1) || (buttonHeight > 500))
                            throw new ConfigException(lineNr, "Invalid height.");
                        break;
                    case KEYWORD_MARGIN:
                        if (linePieces.Length != 2)
                            throw new ConfigException(lineNr, "Row with keyword 'margin' should contain 1 data column.");
                        if (!int.TryParse(linePieces[1], out buttonMargin) || (buttonMargin < 1) || (buttonMargin > 500))
                            throw new ConfigException(lineNr, "Invalid margin.");
                        break;
                    case KEYWORD_FONT:
                        if (linePieces.Length < 3)
                            throw new ConfigException(lineNr, "Row with keyword 'font' should contain at least 2 data columns.");
                        string fontName = linePieces[1];
                        if (!int.TryParse(linePieces[2], out int fontSize) || (fontSize < 1) || (fontSize > 120))
                            throw new ConfigException(lineNr, "Invalid font size.");
                        buttonFontBold = false;
                        buttonFontItalic = false;
                        for (int i = 3; i < linePieces.Length; i++)
                        {
                            if (linePieces[i] == "bold")
                                buttonFontBold = true;
                            if (linePieces[i] == "italic")
                                buttonFontItalic = true;
                        }
                        FontStyle buttonFontStyle = FontStyle.Regular;
                        if (buttonFontBold)
                            buttonFontStyle |= FontStyle.Bold;
                        if (buttonFontItalic)
                            buttonFontStyle |= FontStyle.Italic;
                        buttonFont = new Font(buttonFontName, buttonFontSize, buttonFontStyle);
                        break;
                    case KEYWORD_BUTTON:
                        if (linePieces.Length != 5)
                            throw new ConfigException(lineNr, "Row with keyword 'button' should contain 4 data columns.");
                        string buttonText = linePieces[1];
                        if (!int.TryParse(linePieces[2], out int macroIndex) || (macroIndex < 0))
                            throw new ConfigException(lineNr, "Invalid macro index.");
                        Color buttonBackgroundColor, buttonForegroundColor;
                        try
                        {
                            buttonBackgroundColor = ColorTranslator.FromHtml(linePieces[3]);
                        }
                        catch
                        {
                            throw new ConfigException(lineNr, "Invalid background color.");
                        }
                        try
                        {
                            buttonForegroundColor = ColorTranslator.FromHtml(linePieces[4]);
                        }
                        catch
                        {
                            throw new ConfigException(lineNr, "Invalid foreground color.");
                        }
                        Button newButton = new Button();
                        newButton.Text = buttonText;
                        newButton.BackColor = buttonBackgroundColor;
                        newButton.ForeColor = buttonForegroundColor;
                        newButton.Size = new Size(buttonWidth, buttonHeight);
                        newButton.Margin = new Padding(buttonMargin);
                        newButton.FlatStyle = FlatStyle.Flat;
                        newButton.Font = buttonFont;
                        newButton.Tag = macroIndex;
                        newButton.Click += buttonClickHandler;
                        if (tableLayout.ColumnCount <= currentColumn)
                        {
                            tableLayout.ColumnCount++;
                            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                        }
                        tableLayout.Controls.Add(newButton, currentColumn, currentRow);
                        currentColumn++;
                        break;
                    case KEYWORD_EMPTY:
                        currentColumn++;
                        break;
                    case KEYWORD_NEWLINE:
                        currentRow++;
                        tableLayout.RowCount++;
                        tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                        currentColumn = 0;
                        break;
                    default:
                        throw new ConfigException(lineNr, "Invalid keyword.");
                }
                lineNr++;
            }
            if (destinationEndPoint == null)
                throw new Exception("No IP endpoint provided.");
        }

        private IPEndPoint destinationEndPoint = null;

        private void buttonClickHandler(object sender, EventArgs e)
        {
            int macroIndex = (int)(((Button)sender).Tag);
            IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Any, 9700);
            OscMessage message = new OscMessage(sourceEndPoint, $"/macros/exec/{macroIndex}");
            message.AppendNil();
            OscBundle bundle = new OscBundle(sourceEndPoint);
            bundle.Append(message);
            bundle.Send(destinationEndPoint);
        }

        private class ConfigException : Exception
        {
            public ConfigException(int lineNr, string message) : base($"Line {lineNr}: {message}")
            { }
        }

    }

}
