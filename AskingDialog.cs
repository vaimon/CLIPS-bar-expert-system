using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipsFormsExample
{
    public partial class AskingDialog : Form
    {
        public AskingDialog()
        {
        }
        Dictionary<int, InitialFact> oppositeFacts;
        List<KeyValuePair<int, InitialFact>> facts;
        bool areFeatures;
        public AskingDialog(string description, List<string> answers, bool isRequired)
        {
            InitializeComponent();
            oppositeFacts = new Dictionary<int, InitialFact>();
            this.areFeatures = !isRequired;
            facts = new List<KeyValuePair<int, InitialFact>>();
            foreach (var entry in answers)
            {
                var splitted = entry.Split('-');
                
                var fact = new KeyValuePair<int, InitialFact>(int.Parse(splitted[0]), new InitialFact(splitted[1], InitialFactType.FEATURE));
                if (areFeatures)
                {
                    oppositeFacts.Add(int.Parse(splitted[2]), new InitialFact(splitted[3], InitialFactType.OPPOSITE_FEATURE, certainty: 1.0));
                    fact.Value.oppositeFact = int.Parse(splitted[2]);
                }
                facts.Add(fact);
            }
            labelDescription.Text = description;
            checkedListBox.Items.AddRange(facts.Select(x => new InitialFactWrapper(x)).ToArray());
            
            if (isRequired)
            {
                btnClose.Enabled = false;
            }
            checkedListBox.SelectedIndex = 0;
        }

        public List<KeyValuePair<int, Fact>> SelectedFacts { get; set; }

        private void btnClose_Click(object sender, EventArgs e)
        {
            
            if (areFeatures)
            {
                var selectedFeatures =  checkedListBox.Items.Cast<InitialFactWrapper>().Select(x => x.fact).Where(x => x.Value.certainty > 0).ToList();
                var unselectedFeatures = checkedListBox.Items.Cast<InitialFactWrapper>().Select(x => x.fact).Where(x => !selectedFeatures.Contains(x)).Select(x => oppositeFacts.GetEntry(x.Value.oppositeFact));
                selectedFeatures.AddRange(unselectedFeatures);
                SelectedFacts = selectedFeatures.Select(x => new KeyValuePair<int, Fact>(x.Key, x.Value)).ToList();
            }
            else
            {
                SelectedFacts = checkedListBox.Items.Cast<InitialFactWrapper>().Select(x => x.fact).Where(f => f.Value.certainty > 0).Select(x => new KeyValuePair<int, Fact>(x.Key, x.Value)).ToList();
            }
            Close();
        }
        int lastSelectedIndex = 0;
        private void checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(checkedListBox.SelectedItems.Count > 0)
            {
                btnClose.Enabled = true;
            }
            if(checkedListBox.SelectedIndex != -1)
            {
                lastSelectedIndex = checkedListBox.SelectedIndex;
            }
            trackBar.Value = (int)(facts[lastSelectedIndex].Value.certainty * 100);
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            facts[lastSelectedIndex].Value.certainty = (trackBar.Value * 1.0) / 100;
            checkedListBox.Items[lastSelectedIndex] = new InitialFactWrapper(facts[lastSelectedIndex]);
        }
    }
}
