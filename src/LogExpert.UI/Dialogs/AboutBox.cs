using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;

using LogExpert.Core.Classes;

using Newtonsoft.Json;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class AboutBox : Form
{
    #region Fields

    private readonly Assembly _assembly;

    #endregion

    #region cTor

    public AboutBox ()
    {
        InitializeComponent();

        LoadResources();
        usedComponentsDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _assembly = Assembly.GetExecutingAssembly();

        Text = $@"About {AssemblyTitle}";
        labelProductName.Text = AssemblyProduct;
        labelVersion.Text = AssemblyVersion;
        labelCopyright.Text = AssemblyCopyright;
        var link = "https://github.com/LogExperts/LogExpert";
        _ = linkLabelURL.Links.Add(new LinkLabel.Link(0, link.Length, link));
        LoadUsedComponents();
    }

    //Name, Version, License, Download, Source

    private void LoadUsedComponents ()
    {
        var json = File.ReadAllText($"{Application.StartupPath}files\\json\\usedComponents.json");
        var usedComponents = JsonConvert.DeserializeObject<UsedComponents[]>(json);
        usedComponents = usedComponents?.OrderBy(x => x.PackageId).ToArray();
        usedComponentsDataGrid.DataSource = usedComponents;

    }


    private void LoadResources ()
    {
        logoPictureBox.Image = Resources.LogLover;
    }

    #endregion

    #region Properties

    public string AssemblyTitle
    {
        get
        {
            var attributes = _assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (!string.IsNullOrEmpty(titleAttribute.Title))
                {
                    return titleAttribute.Title;
                }
            }
            return Path.GetFileNameWithoutExtension(_assembly.Location);
        }
    }

    public string AssemblyVersion
    {
        get
        {
            var assembly = _assembly.GetName();

            return assembly.Version != null
                ? $"{assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}.{assembly.Version.Revision}"
                : "0.0.0.0";
        }

    }

    public string AssemblyDescription
    {
        get
        {
            var attributes = _assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);

            return attributes.Length == 0
                ? string.Empty
                : ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    public string AssemblyProduct
    {
        get
        {
            var attributes = _assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0
                ? string.Empty
                : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public string AssemblyCopyright
    {
        get
        {
            var attributes = _assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0
                ? string.Empty
                : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    #endregion

    #region Events handler

    private void OnLinkLabelURLClicked (object sender, LinkLabelLinkClickedEventArgs e)
    {
        var target = string.Empty;

        if (e.Link != null)
        {
            target = e.Link.LinkData as string;
        }

        _ = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = target,
        });
    }

    #endregion
}