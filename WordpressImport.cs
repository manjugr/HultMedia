// Type: Sitecore.Modules.WeBlog.sitecore.shell.Applications.WeBlog.WordpressImport
// Assembly: Sitecore.Modules.WeBlog, Version=2.2.0.14504, Culture=neutral, PublicKeyToken=null
// Assembly location: C:\SVN\branches\dev\AWS\HultMedia\bin\Sitecore.Modules.WeBlog.dll

using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Modules.WeBlog;
using Sitecore.Modules.WeBlog.Import;
using Sitecore.Modules.WeBlog.Items.WeBlog;
using Sitecore.Shell.Applications.Install;
using Sitecore.Shell.Applications.Install.Dialogs;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sitecore.Modules.WeBlog.sitecore.shell.Applications.WeBlog
{
  public class WordpressImport : WizardForm
  {
    protected Edit WordpressXmlFile;
    protected Groupbox ImportOptionsPane;
    protected DataContext DataContext;
    protected TreeviewEx Treeview;
    protected Literal litSummaryName;
    protected Literal litSummaryEmail;
    protected Literal litSummaryPath;
    protected Literal litSummaryWordpressXML;
    protected Literal litSummaryPosts;
    protected Literal litSummaryCategories;
    protected Literal litSummaryComments;
    protected Literal litSummaryTags;
    protected Literal StatusMessage;
    protected Literal ProgressMessage;
    protected Literal Status;
    protected Memo ResultText;
    protected Border ResultLabel;
    protected Border ShowResultPane;
    protected Edit litSettingsName;
    protected Edit litSettingsEmail;
    protected Edit JobHandle;
    protected Checkbox ImportPosts;
    protected Checkbox ImportCategories;
    protected Checkbox ImportComments;
    protected Checkbox ImportTags;
    protected Database db;

    public WordpressImport()
    {
      this.db = ContentHelper.GetContentDatabase();
      base.\u002Ector();
    }

    protected override void OnLoad(EventArgs e)
    {
      this.ImportOptionsPane.Visible = false;
      this.DataContext.GetFromQueryString();
      base.OnLoad(e);
    }

    protected override void ActivePageChanged(string page, string oldPage)
    {
      Assert.ArgumentNotNull((object) page, "page");
      Assert.ArgumentNotNull((object) oldPage, "oldPage");
      base.ActivePageChanged(page, oldPage);
      if (page == "Import" && !string.IsNullOrEmpty(this.WordpressXmlFile.Value))
      {
        this.ImportOptionsPane.Visible = true;
      }
      else
      {
        if (page == "Summary")
        {
          this.litSummaryName.Text = this.litSettingsName.Value;
          this.litSummaryEmail.Text = this.litSettingsEmail.Value;
          this.litSummaryPath.Text = Enumerable.First<Item>((IEnumerable<Item>) this.Treeview.GetSelectedItems()).Paths.FullPath;
          this.litSummaryWordpressXML.Text = this.WordpressXmlFile.Value;
          this.litSummaryCategories.Text = this.ImportCategories.Checked ? "Yes" : "No";
          this.litSummaryComments.Text = this.ImportComments.Checked ? "Yes" : "No";
          this.litSummaryPosts.Text = this.ImportPosts.Checked ? "Yes" : "No";
          this.litSummaryTags.Text = this.ImportTags.Checked ? "Yes" : "No";
        }
        this.NextButton.Header = "Next >";
        if (page == "Summary")
          this.NextButton.Header = "Start Import";
        if (page == "ImportJob")
        {
          this.BackButton.Disabled = true;
          this.NextButton.Disabled = true;
          this.StartImport();
        }
        if (!(page == "LastPage"))
          return;
        this.NextButton.Disabled = true;
        this.BackButton.Disabled = true;
        this.CancelButton.Disabled = false;
      }
    }

    private void StartImport()
    {
      Job job = JobManager.Start(new JobOptions("Creating and importing blog", "WeBlog", Context.Site.Name, (object) this, "ImportBlog"));
      job.Status.Total = 0L;
      this.JobHandle.Value = job.Handle.ToString();
      Context.ClientPage.ClientResponse.Timer("CheckStatus", 500);
    }

    private void ImportBlog()
    {
      this.LogMessage("Reading import file");
      List<WpPost> listWordpressPosts = WpImportManager.Import(string.Format("{0}\\{1}", (object) ApplicationContext.PackagePath, (object) this.WordpressXmlFile.Value), this.ImportComments.Checked, this.ImportCategories.Checked, this.ImportTags.Checked);
      this.LogMessage("Creating blog");
      BlogHomeItem blogHomeItem = (BlogHomeItem) this.db.GetItem(this.litSummaryPath.Text).Add(ItemUtil.ProposeValidItemName(this.litSettingsName.Value), this.db.Branches.GetMaster(Settings.BlogBranchID));
      blogHomeItem.BeginEdit();
      blogHomeItem.Email.Field.Value = this.litSettingsEmail.Value;
      blogHomeItem.EndEdit();
      this.LogMessage("Importing posts");
      this.LogTotal(listWordpressPosts.Count);
      // ISSUE: method pointer
      WpImportManager.ImportPosts((Item) blogHomeItem, listWordpressPosts, this.db, new System.Action<string, int>((object) this, __methodptr(\u003CImportBlog\u003Eb__0)));
    }

    protected void CheckStatus()
    {
      Job job = this.GetJob();
      if (job == null)
        return;
      if (job.Status.Messages.Count >= 1)
        this.StatusMessage.Text = job.Status.Messages[job.Status.Messages.Count - 1];
      this.ProgressMessage.Text = StringExtensions.FormatWith("Processed {0} entries of {1} total", (object) job.Status.Processed, (object) job.Status.Total);
      if (job.IsDone)
      {
        if (job.Status.Failed)
        {
          this.Status.Text = "Import failed";
          foreach (string str1 in job.Status.Messages)
          {
            Memo memo = this.ResultText;
            string str2 = memo.Value + str1 + "\r\n";
            memo.Value = str2;
          }
        }
        else
          this.Status.Text = this.ProgressMessage.Text;
        this.Active = "LastPage";
      }
      else
        Context.ClientPage.ClientResponse.Timer("CheckStatus", 500);
    }

    protected void OK_Click()
    {
      if (this.Treeview.GetSelectionItem() != null)
        return;
      SheerResponse.Alert("Select an item.", new string[0]);
    }

    protected void ShowResult()
    {
      this.ShowResultPane.Visible = false;
      this.ResultText.Visible = true;
      this.ResultLabel.Visible = true;
    }

    private Job GetJob()
    {
      Handle handle = Handle.Parse(this.JobHandle.Value);
      if (handle != null)
        return JobManager.GetJob(handle);
      else
        return (Job) null;
    }

    private void LogMessage(string message)
    {
      Job job = this.GetJob();
      if (job == null)
        return;
      job.Status.Messages.Add(message);
    }

    private void LogProgress(int count)
    {
      Job job = this.GetJob();
      if (job == null)
        return;
      job.Status.Processed = (long) count;
    }

    private void LogTotal(int total)
    {
      Job job = this.GetJob();
      if (job == null)
        return;
      job.Status.Total = (long) total;
    }

    [HandleMessage("installer:upload", true)]
    protected void Upload(ClientPipelineArgs args)
    {
      if (!args.IsPostBack)
      {
        UploadPackageForm.Show(ApplicationContext.PackagePath, true);
        args.WaitForPostBack();
      }
      else
      {
        if (!args.Result.StartsWith("ok:"))
          return;
        this.WordpressXmlFile.Value = args.Result.Substring("ok:".Length);
      }
    }

    [HandleMessage("installer:browse", true)]
    protected void Browse(ClientPipelineArgs args)
    {
      if (args.IsPostBack && args.HasResult)
      {
        if (this.WordpressXmlFile == null)
          return;
        this.WordpressXmlFile.Value = args.Result;
        this.ImportOptionsPane.Visible = true;
      }
      else
      {
        BrowseDialog.BrowseForOpen(ApplicationContext.PackagePath, "*.xml", "Open XML file", "Select the file that you want to open.", "People/16x16/box.png");
        args.WaitForPostBack();
      }
    }

    [CompilerGenerated]
    private void \u003CImportBlog\u003Eb__0(string itemName, int count)
    {
      this.LogMessage("Importing entry " + itemName);
      this.LogProgress(count);
    }
  }
}
