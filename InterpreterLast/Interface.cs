using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InterpreterLast
{
    public partial class Interface : Form
    {
        public Interface()
        {
            InitializeComponent();
        }

        private Dictionary<string, (string, object)> variables = new Dictionary<string, (string, object)>();

        private void ExecuteCode(string code)
        {
            bool insideCodeBlock = false;
            StringBuilder output = new StringBuilder();

            string[] lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string cleanLine = Regex.Replace(line, @"#.*", "").Trim();
                if (string.IsNullOrWhiteSpace(cleanLine))
                    continue;

                if (cleanLine == "BEGIN CODE")
                {
                    insideCodeBlock = true;
                    continue;
                }

                if (cleanLine == "END CODE")
                {
                    insideCodeBlock = false;
                    continue;
                }

                if (insideCodeBlock)
                {
                    if (cleanLine.StartsWith("DISPLAY:"))
                    {
                        string textToDisplay = cleanLine.Substring("DISPLAY:".Length).Trim();
                        output.AppendLine(ProcessDisplayStatement(textToDisplay));
                    }
                    else if (cleanLine.Contains("="))
                    {
                        ParseVariableDeclaration(cleanLine);
                    }
                    else
                    {
                        output.AppendLine(EvaluateArithmeticExpression(cleanLine).ToString());
                    }
                }
            }

            MessageBox.Show(output.ToString(), "Output");
        }

        private void ParseVariableDeclaration(string line)
        {
            string[] tokens = line.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3)
            {
                MessageBox.Show("Invalid variable declaration: " + line, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string type = tokens[0];
            for (int i = 1; i < tokens.Length; i += 2)
            {
                string name = tokens[i];
                object value;
                if (tokens.Length > i + 2 && tokens[i + 1] == "+" && tokens[i + 2] == tokens[i + 3])
                {
                    value = EvaluateArithmeticExpression(tokens[i + 2] + tokens[i + 1] + tokens[i + 3]);
                    i += 3;
                }
                else
                {
                    value = ParseValue(tokens[i + 1], type);
                }
                variables[name] = (type, value);
            }
        }

        private object ParseValue(string value, string type)
        {
            switch (type)
            {
                case "INT":
                    return int.Parse(value);
                case "CHAR":
                    return value.Trim('\'')[0];
                case "BOOL":
                    return value.Trim('"').ToUpper() == "TRUE";
                case "FLOAT":
                    return float.Parse(value);
                default:
                    MessageBox.Show("Invalid variable type: " + type, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
            }
        }

        private object EvaluateArithmeticExpression(string expression)
        {
            string[] tokens = Regex.Split(expression, @"(?<=[-+*/])|(?=[-+*/])");

            List<object> values = new List<object>();
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out int intValue))
                {
                    values.Add(intValue);
                }
                else if (float.TryParse(token, out float floatValue))
                {
                    values.Add(floatValue);
                }
                else
                {
                    MessageBox.Show("Invalid number format in expression: " + expression, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }

            float result = Convert.ToSingle(values[0]);
            for (int i = 1; i < values.Count; i += 2)
            {
                string op = (string)values[i];
                float nextValue = Convert.ToSingle(values[i + 1]);
                switch (op)
                {
                    case "+":
                        result += nextValue;
                        break;
                    case "-":
                        result -= nextValue;
                        break;
                    case "*":
                        result *= nextValue;
                        break;
                    case "/":
                        result /= nextValue;
                        break;
                    default:
                        MessageBox.Show("Invalid operator: " + op, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                }
            }

            return result;
        }

        private string ProcessDisplayStatement(string statement)
        {
            StringBuilder result = new StringBuilder();
            string[] tokens = statement.Split('&');
            foreach (string token in tokens)
            {
                if (token.Trim().StartsWith("$"))
                {
                    result.AppendLine();
                }
                else if (token.Trim().StartsWith("[") || token.Trim().EndsWith("]"))
                {
                    continue;
                }
                else
                {
                    string trimmedToken = token.Trim();
                    if (variables.ContainsKey(trimmedToken))
                    {
                        var (_, value) = variables[trimmedToken];
                        if (value != null)
                        {
                            result.Append(value.ToString());
                        }
                        else
                        {
                            result.Append("NULL");
                        }
                    }
                    else if (IsArithmeticExpression(trimmedToken))
                    {
                        string[] parts = Regex.Split(trimmedToken, @"(?<=[-+*/])|(?=[-+*/])");
                        if (parts.Length == 3)
                        {
                            string operatorStr = parts[1];
                            object leftValue = GetVariableValue(parts[0]);
                            object rightValue = GetVariableValue(parts[2]);
                            object resultValue = PerformArithmeticOperation(operatorStr, leftValue, rightValue);
                            result.Append(resultValue.ToString());
                        }
                    }
                    else
                    {
                        result.Append(trimmedToken.Replace("\"", ""));
                    }
                }
            }
            return result.ToString();
        }

        private bool IsArithmeticExpression(string token)
        {
            return token.Contains('+') || token.Contains('-') || token.Contains('*') || token.Contains('/');
        }

        private object PerformArithmeticOperation(string operatorStr, object leftValue, object rightValue)
        {
            if (leftValue is int leftInt && rightValue is int rightInt)
            {
                switch (operatorStr)
                {
                    case "+": return (int)(leftInt + rightInt);
                    case "-": return (int)(leftInt - rightInt);
                    case "*": return (int)(leftInt * rightInt);
                    case "/":
                        if (rightInt == 0)
                        {
                            MessageBox.Show("Division by zero error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                        return (int)(leftInt / rightInt);
                    case "%": return (int)(leftInt % rightInt);
                    default:
                        MessageBox.Show("Invalid operator: " + operatorStr, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                }
            }
            else if (leftValue is float leftFloat && rightValue is float rightFloat)
            {
                switch (operatorStr)
                {
                    case "+": return (float)(leftFloat + rightFloat);
                    case "-": return (float)(leftFloat - rightFloat);
                    case "*": return (float)(leftFloat * rightFloat);
                    case "/":
                        if (rightFloat == 0)
                        {
                            MessageBox.Show("Division by zero error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                        return (float)(leftFloat / rightFloat);
                    case "%": return (float)(leftFloat % rightFloat);
                    default:
                        MessageBox.Show("Invalid operator: " + operatorStr, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                }
            }
            else if (leftValue is float leftF && rightValue is int rightI)
            {
                switch (operatorStr)
                {
                    case "+": return (float)(leftF + (int)rightI);
                    case "-": return (float)(leftF - (int)rightI);
                    case "*": return (float)(leftF * (int)rightI);
                    case "/":
                        if ((int)rightI == 0)
                        {
                            MessageBox.Show("Division by zero error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                        return (float)(leftF / (int)rightI);
                    default:
                        MessageBox.Show("Invalid operator: " + operatorStr, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                }
            }
            else if (leftValue is int leftI && rightValue is float rightF)
            {
                switch (operatorStr)
                {
                    case "+": return (float)((float)leftI + rightF);
                    case "-": return (float)((float)leftI - rightF);
                    case "*": return (float)((float)leftI * rightF);
                    case "/":
                        if (rightF == 0)
                        {
                            MessageBox.Show("Division by zero error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                        return (float)((float)leftI / rightF);
                    default:
                        MessageBox.Show("Invalid operator: " + operatorStr, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                }
            }

            MessageBox.Show("Invalid operand types for arithmetic operation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        private object GetVariableValue(string variableName)
        {
            variableName = variableName.Trim().Replace("(", "").Replace(")", "").Replace("\"", "").Replace("\'", "");
            if (int.TryParse(variableName, out int intValue))
            {
                return intValue;
            }
            else if (variables.ContainsKey(variableName))
            {
                var (_, value) = variables[variableName];
                return value;
            }
            MessageBox.Show("Variable not found: " + variableName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        private void RunInterpreter()
        {
            string code = txtCode.Text;
            ExecuteCode(code);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            RunInterpreter();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtCode.Clear();
        }
    }
}
