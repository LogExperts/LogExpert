using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RegexColumnizer;

public partial class RegexColumnizerConfigDialog : Form
{
    public RegexColumnizerConfigDialog()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        ResumeLayout();
    }

    public RegexColumnizerConfig Config { get; set; }

    private void OnBtnOkClick(object sender, EventArgs e)
    {
        if (Check())
        {
            Config.Expression = tbExpression.Text;
            Config.Name = tbName.Text;
        }

    }

    private void RegexColumnizerConfigDialog_Load(object sender, EventArgs e)
    {
        tbExpression.Text = Config.Expression;
        tbName.Text = Config.Name;
    }

    private void OnButtonCheckClick(object sender, EventArgs e)
    {
        Check();
    }

    private bool Check()
    {
        DataTable table = new();

        try
        {
            Regex regex = new(tbExpression.Text);
            var groupNames = regex.GetGroupNames();
            var offset = groupNames.Length > 1 ? 1 : 0;

            for (var i = offset; i < groupNames.Length; i++)
            {
                table.Columns.Add(groupNames[i]);
            }

            if (!string.IsNullOrEmpty(tbTestLine.Text))
            {
                Match match = regex.Match(tbTestLine.Text);
                var row = table.NewRow();
                var values = match.Groups.OfType<Group>().Skip(offset).Select(group => group.Value).Cast<object>().ToArray();
                row.ItemArray = values;
                table.Rows.Add(row);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Invalid Regex !{Environment.NewLine}{ex.Message}", @"Regex Columnizer Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            dataGridView1.DataSource = table;
        }
    }
}
