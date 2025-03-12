using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace LogExpert.Dialogs
{
    internal partial class AboutBox : Form
    {
        #region Fields

        private readonly Assembly _assembly;

        #endregion

        #region cTor

        public AboutBox()
        {
            InitializeComponent();

            _assembly = Assembly.GetExecutingAssembly();

            Text = $@"About {AssemblyTitle}";
            labelProductName.Text = AssemblyProduct;
            labelVersion.Text = AssemblyVersion;
            labelCopyright.Text = AssemblyCopyright;
            //this.labelCompanyName.Text = AssemblyCompany;
            textBoxDescription.Text = AssemblyDescription +
                                           "\r\n\r\nCredits:\r\n\r\n" +
                                           "DockPanel control (c) 2007 Weifen Luo \r\n" +
                                           "Early bird test: Mathias Dräger\r\n" +
                                           "\r\n" +
                                           "LogExpert uses modules from:\r\n" +
                                           "http://sourceforge.net/projects/dockpanelsuite/\r\n" +
                                           "http://www.xml-rpc.net/";
            string link = "https://github.com/zarunbal/LogExpert/";
            linkLabelURL.Links.Add(new LinkLabel.Link(0, link.Length, link));
        }

        #endregion

        #region Properties

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = _assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
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
                AssemblyName assembly = _assembly.GetName();
                string version = $"{assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}.{assembly.Version.Revision}";
                if (assembly.Version.Revision >= 9000)
                {
                    version += " Testrelease";
                }

                return version;
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = _assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = _assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = _assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        #endregion

        #region Events handler

        private void OnLinkLabelURLClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string target = e.Link.LinkData as string;
            System.Diagnostics.Process.Start(target);
        }

        #endregion
    }
}