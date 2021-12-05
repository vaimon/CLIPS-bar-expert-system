using System;
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
    }
}