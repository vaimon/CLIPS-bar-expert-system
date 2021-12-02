using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CLIPSNET;


namespace ClipsFormsExample
{
    public partial class ClipsFormsExample : Form
    {
        private CLIPSNET.Environment clips = new CLIPSNET.Environment();
        Dictionary<int, Fact> facts;
        Dictionary<int, Rule> rules;
        private string clipsCode = "";

        String dbFolderName = "C:\\Code\\CLIPSForms\\db";

        public ClipsFormsExample()
        {
            InitializeComponent();
            facts = new Dictionary<int, Fact>();
            rules = new Dictionary<int, Rule>();
            nextButton.Text = "Старт";
            setState(false);
        }

        void setState(bool state)
        {
            saveAsButton.Enabled = state;
            fontButton.Enabled = state;
            resetButton.Enabled = state;
            nextButton.Enabled = state;
        }

        string factToClipsFact(int factNumber)
        {
            return $"(fact (num {factNumber})(description \"{facts[factNumber].factDescription.Replace('&', 'и').Replace('(', '/').Replace(')', '/')}\"))";
        }

        string factToReadableFact(int factNumber)
        {
            return $"/f-{factNumber}: {facts[factNumber].factDescription.Replace('&', 'и').Replace('(', '/').Replace(')', '/')}/";
        }
        private string generateCLIPScode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(File.ReadAllText(dbFolderName + "\\base.clp"));
            foreach (var rule in rules)
            {
                sb.AppendLine($"(defrule r-{rule.Key}");
                if (facts[rule.Value.conclusion] is FiniteFact)
                {

                    sb.AppendLine("(declare (salience 39))");
                }
                else
                {
                    sb.AppendLine("(declare (salience 40))");
                }
                for (var i = 0; i < rule.Value.premises.Count; i++)
                {
                    sb.AppendLine(factToClipsFact(rule.Value.premises[i]));
                }

                sb.AppendLine("=>");
                sb.AppendLine($"(assert {factToClipsFact(rule.Value.conclusion)})");
                if (facts[rule.Value.conclusion] is FiniteFact)
                {
                    sb.AppendLine($"(assert (appendmessagehalt \"#[Применили правило #{rule.Key}: {string.Join(" и ", rule.Value.premises.Select(x => factToReadableFact(x)).ToArray())} \n => \n {factToReadableFact(rule.Value.conclusion)},\n или, если по человечески: {rule.Value.comment.Replace('&', 'и').Replace('(', '/').Replace(')', '/')}]\n\"))");

                }
                else
                {
                    sb.AppendLine($"(assert (appendmessagehalt \"[Применили правило #{rule.Key}: {string.Join(" и ", rule.Value.premises.Select(x => factToReadableFact(x)).ToArray())} \n => \n {factToReadableFact(rule.Value.conclusion)},\n или, если по человечески: {rule.Value.comment.Replace('&', 'и').Replace('(', '/').Replace(')', '/')}]\n\"))");

                }
                sb.AppendLine(")");
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        InitialFactType parseType(String type)
        {
            switch (type)
            {
                case "fia": return InitialFactType.DRINK_TYPE;
                case "fib": return InitialFactType.BUDGET;
                case "fic": return InitialFactType.COMPANY_SIZE;
                case "fil": return InitialFactType.LOCATION;
                case "fi": return InitialFactType.FEATURE;
                case "fn": return InitialFactType.OPPOSITE_FEATURE;
                default: throw new Exception("Типы фактов всё сломали :(");
            }
        }

        void loadDB()
        {
            using (var sr = new StreamReader(dbFolderName + "\\db.txt"))
            {
                while (!sr.EndOfStream)
                {
                    var data = sr.ReadLine().Split(';');
                    var id = data[0].Split('-');
                    if (id[0].Equals("f"))
                    {
                        facts.Add(int.Parse(id[1]), new Fact(data[1]));
                    }
                    else if (id[0].StartsWith("f"))
                    {
                        if (id[0].Equals("ff"))
                        {
                            facts.Add(int.Parse(id[1]), new FiniteFact(data[1]));
                            continue;
                        }

                        var fact = new InitialFact(data[1], parseType(id[0]));
                        if (fact.factType == InitialFactType.OPPOSITE_FEATURE)
                        {
                            (facts[int.Parse(id[2])] as InitialFact).oppositeFact = int.Parse(id[1]);
                            facts.Add(int.Parse(id[1]), fact);
                            continue;
                        }

                        facts.Add(int.Parse(id[1]), fact);
                    }
                    else if (data[0].StartsWith("r"))
                    {
                        if (data.Count() != 4)
                        {
                            throw new ArgumentException("В правиле поплыла структура");
                        }

                        var premises = data[1].Split(',').Select(x => int.Parse(x.Split('-')[1])).ToList();
                        rules.Add(int.Parse(id[1]), new Rule(premises, int.Parse(data[2].Split('-')[1]), data[3]));
                    }
                    else
                    {
                        throw new Exception("Something went wrong");
                    }
                }
            }
        }

        void askSomeQuestion(string message, List<string> answers)
        {
            List<KeyValuePair<int, Fact>> selectedFacts;
            AskingDialog form = new AskingDialog();
            if (message.EndsWith("features"))
            {
                form = new AskingDialog("Можете выбрать интересующие вас фичи, если хотите:", answers, false);
            }
            else if (message.EndsWith("location"))
            {
                form = new AskingDialog("Для продолжения выберите место, где хотите выпить:", answers, true);
            }
            else if (message.EndsWith("company"))
            {
                form = new AskingDialog("Для продолжения выберите размер компании:", answers, true);
            }
            else if (message.EndsWith("budget"))
            {
                form = new AskingDialog("Для продолжения выберите бюджет:", answers, true);
            }
            else if (message.EndsWith("drinks"))
            {
                form = new AskingDialog("Для продолжения выберите что будете пить:", answers, true);
            }
            else
            {
                throw new Exception("Упс... Кажется, я сломал клипс...");
            }
            form.ShowDialog(this);
            selectedFacts = form.SelectedFacts;
            foreach (var pair in selectedFacts)
            {
                clips.Eval($"(assert {factToClipsFact(pair.Key)})");
            }
        }

        private bool HandleResponse()
        {
            //  Вытаскиаваем факт из ЭС
            String evalStr = "(find-fact ((?f ioproxy)) TRUE)";
            FactAddressValue fv = (FactAddressValue) ((MultifieldValue) clips.Eval(evalStr))[0];

            MultifieldValue damf = (MultifieldValue) fv["messages"];
            MultifieldValue vamf = (MultifieldValue) fv["answers"];
            if (damf.Count == 0)
            {
                return false;
            }
            //outputBox.Text += "Новая итерация : " + System.Environment.NewLine;
            for (int i = 0; i < damf.Count; i++)
            {
                LexemeValue da = (LexemeValue) damf[i];
                byte[] bytes = Encoding.Default.GetBytes(da.Value);
                string message = Encoding.UTF8.GetString(bytes);
                if (message.StartsWith("#ask"))
                {
                    var phrases = new List<string>();
                    if (vamf.Count > 0)
                    {
                        outputBox.AppendText("----------------------------------------------------" + System.Environment.NewLine, Color.DarkBlue);
                        for (int j = 0; j < vamf.Count; j++)
                        {
                            //  Варианты !!!!!
                            LexemeValue va = (LexemeValue)vamf[j];
                            byte[] bytess = Encoding.Default.GetBytes(va.Value);
                            string messagee = Encoding.UTF8.GetString(bytess);
                            phrases.Add(messagee);
                            outputBox.AppendText("Добавлен вариант для распознавания " + messagee + System.Environment.NewLine, Color.DarkBlue);
                        }
                        outputBox.AppendText("----------------------------------------------------" + System.Environment.NewLine, Color.DarkBlue);
                    }
                    askSomeQuestion(message, phrases);
                    outputBox.AppendText(message + System.Environment.NewLine, Color.DarkBlue);
                }
                else if (message.StartsWith("#"))
                {

                    outputBox.AppendText(message + System.Environment.NewLine, Color.DarkRed);
                }
                else
                {
                    outputBox.AppendText(message + System.Environment.NewLine, Color.Black);
                }
                
                outputBox.SelectionStart = outputBox.Text.Length;
                outputBox.ScrollToCaret();
            }
            

            //if (vamf.Count == 0)
                clips.Eval("(assert (clearmessage))");
            return true;
        }
        bool isFirstTime = true;
        private void nextBtn_Click(object sender, EventArgs e)
        {
            if (isFirstTime)
            {
                nextButton.Text = "Дальше";
                isFirstTime = false;
            }
            clips.Run();
            while (HandleResponse())
            {
                clips.Run();
            }
            outputBox.AppendText("Вывод завершён!\n", Color.DarkGreen);
        }

        private void resetBtn_Click(object sender, EventArgs e)
        {
            outputBox.Text = "Выполнены команды Clear и Reset." + System.Environment.NewLine;
            //  Здесь сохранение в файл, и потом инициализация через него
            clips.Clear();

            //  Так тоже можно - без промежуточного вывода в файл
            clips.LoadFromString(clipsCode);

            clips.Reset();
        }

        private void openFile_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                dbFolderName = folderBrowserDialog1.SelectedPath;
                loadDB();
                codeBox.Text = clipsCode = generateCLIPScode();
                setState(true);
            }
        }

        private void fontSelect_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                codeBox.Font = fontDialog1.Font;
                outputBox.Font = fontDialog1.Font;
            }
        }

        private void saveAsButton_Click(object sender, EventArgs e)
        {
            clipsSaveFileDialog.FileName = "bar_data.clp";
            if (clipsSaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(clipsSaveFileDialog.FileName, codeBox.Text);
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void outputBox_TextChanged(object sender, EventArgs e)
        {

        }
    }


    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}