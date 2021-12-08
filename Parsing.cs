using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ClipsFormsExample
{
    public partial class ClipsFormsExample
    {
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
                        if (data.Count() != 5)
                        {
                            throw new ArgumentException("В правиле поплыла структура");
                        }

                        var premises = data[1].Split(',').Select(x => int.Parse(x.Split('-')[1])).ToList();
                        rules.Add(int.Parse(id[1]), new Rule(premises, int.Parse(data[2].Split('-')[1]), data[3],processRuleCertainty(data[4])));
                    }
                    else
                    {
                        throw new Exception("Something went wrong");
                    }
                }
            }
        }

        double processRuleCertainty(string input)
        {
            var original = double.Parse(input, CultureInfo.InvariantCulture);
            var scaled = (2 - original) * original; // Увеличиваем на (1 - x)%
            if (scaled > 1)
            {
                return 1;
            }
            return scaled;
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

                StringBuilder sbCertainty = new StringBuilder();
                for (var i = 0; i < rule.Value.premises.Count; i++)
                {
                    sb.AppendLine($"(fact (num {rule.Value.premises[i]})(description ?desc{i})(certainty ?cert{i}))");
                    sbCertainty.Append($"?cert{i} ");
                }
                
                sb.AppendLine("=>");
                sb.AppendLine($"(bind ?rule-cert (* (min {sbCertainty.ToString()}) {rule.Value.ruleCertainty.ToString(CultureInfo.InvariantCulture)}))");
                sb.AppendLine($"(bind ?ex-fact (nth$ 1 (find-fact ((?f fact))(eq ?f:num {rule.Value.conclusion}))))");
                sb.AppendLine($"(if (neq ?ex-fact nil)");
                sb.AppendLine("then (bind ?ex-cert (fact-slot-value ?ex-fact certainty)) (modify ?ex-fact (certainty (-(+ ?ex-cert ?rule-cert)(* ?ex-cert ?rule-cert))))");
                sb.AppendLine($"else (assert (fact (num {rule.Value.conclusion})(description \"{facts[rule.Value.conclusion].factDescription}\")(certainty ?rule-cert)))\n)");
                if (facts[rule.Value.conclusion] is FiniteFact)
                {
                    sb.Append($"(assert (appendmessagehalt (str-cat\"#");
                }
                else
                {
                    sb.Append($"(assert (appendmessagehalt (str-cat\"");
                }
                sb.AppendLine($"[Применили правило #{rule.Key}:");
                for (int i = 0; i < rule.Value.premises.Count; i++)
                {
                    sb.AppendLine($"/f-{rule.Value.premises[i]}: \" ?desc{i} \" [~\" ?cert{i} \"]/");
                }
                sb.AppendLine($"=> \n /f-{rule.Value.conclusion}: |{facts[rule.Value.conclusion].factDescription}| [|\" ?rule-cert \"|]/,\n или, если по человечески: {rule.Value.comment}]\")))");
                sb.AppendLine(")");
                sb.AppendLine("");
            }
            return sb.ToString();
        }
    }
}