using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Examine;
using umbraco.BasePages;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.template;
using umbraco.cms.businesslogic.web;
using umbraco.presentation.nodeFactory;
using Umbraco.Core.Services;

public partial class usercontrols_StandardWebsiteInstall : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Find Pages (homepage, etc)

        if (!Page.IsPostBack)
        {


            Umbraco.Core.Services.ContentService contentService = new Umbraco.Core.Services.ContentService();
            Umbraco.Core.Services.FileService fileService = new Umbraco.Core.Services.FileService();

            var root = contentService.GetRootContent().Where(x => x.Name == "Home").FirstOrDefault();

            //Document root = Document.GetRootDocuments().Where(x => x.Text == "Home").FirstOrDefault();

            // Add Public Permissions
            int clientAreaId = -1;
            int loginPageId = -1;
            int errorPageId = -1;
            int contentPanelsId = -1;
            int slideShowId = -1;

            root.SetValue("primaryNavigation", root.Id + "");
            root.SetValue("headerNavigation", "");
            root.SetValue("slideshow", "");

            foreach (var doc in contentService.GetChildren(root.Id))
            {
                // Update Navigation
                if (doc.Name == "Home" ||
                    doc.Name == "About" ||
                    doc.Name == "Products" ||
                    doc.Name == "News" ||
                    doc.Name == "Clients" ||
                    doc.Name == "Contact Us")
                {
                    if (root.GetValue<string>("primaryNavigation") == null ||
                        root.GetValue<string>("primaryNavigation").Length == 0)
                    {
                        root.SetValue("primaryNavigation", doc.Id + "");
                    }
                    else
                    {
                        root.SetValue("primaryNavigation", root.GetValue<string>("primaryNavigation") + "," + doc.Id);
                    }

                }

                if (doc.Name == "Sitemap" ||
                   doc.Name == "Client Area")
                {
                    if (root.GetValue<string>("headerNavigation") == null ||
                        root.GetValue<string>("headerNavigation").Length == 0)
                    {
                        root.SetValue("headerNavigation", doc.Id + "");
                    }
                    else
                    {
                        root.SetValue("headerNavigation", root.GetValue<string>("headerNavigation") + "," + doc.Id);
                    }

                }

                if (doc.Name == "Slideshow")
                {
                    foreach (var slide in contentService.GetChildren(doc.Id))
                    {
                        if (root.GetValue<string>("slideshow") == null ||
                            root.GetValue<string>("slideshow").Length == 0)
                        {
                            root.SetValue("slideshow", slide.Id + "");
                        }
                        else
                        {
                            root.SetValue("slideshow", root.GetValue<string>("slideshow") + "," + slide.Id);
                        }
                    }
                }

                // Fix templates
                if (doc.Name == "Search")
                {
                    doc.Template = fileService.GetTemplate("Search");
                    contentService.Save(doc);
                }

                if (doc.Name == "News")
                {
                    doc.Template = fileService.GetTemplate("Articles");
                    contentService.Save(doc);
                }

                if (doc.Name == "Sitemap")
                {
                    doc.Template = fileService.GetTemplate("Sitemap");
                    contentService.Save(doc);
                }

                if (doc.Name == "Login")
                {
                    doc.Template = fileService.GetTemplate("Login");
                    contentService.Save(doc);
                }


                // Store values for updating Ultimate picker
                if (doc.Name == "Client Area")
                {
                    clientAreaId = doc.Id;
                }
                else if (doc.Name == "Login")
                {
                    loginPageId = doc.Id;
                }
                else if (doc.Name == "Insufficent Access")
                {
                    errorPageId = doc.Id;
                }
                else if (doc.Name == "Content Panels")
                {
                    contentPanelsId = doc.Id;
                }
                else if (doc.Name == "Slideshow")
                {
                    slideShowId = doc.Id;
                }

            }

            // Republish all nodes
            contentService.SaveAndPublish(root);
            contentService.PublishWithChildren(root);
            //root.PublishWithChildrenWithResult(new User(0));

            umbraco.library.RefreshContent(); 
    

            // Move favicon
            try
            {
                File.Move(Server.MapPath("~/images").TrimEnd('\\') + "\\favicon.ico", Server.MapPath("~/").TrimEnd('\\') + "\\favicon.ico");
            }
            catch (Exception ex)
            {
            }

            // Setup Client area
            SetupClientArea(clientAreaId, loginPageId, errorPageId);

            // Reindex Examine
            ExamineManager.Instance.IndexProviderCollection["ExternalIndexer"].RebuildIndex();
        }
    }

    private void SetupClientArea(int clientAreaId, int loginPageId, int errorPageId)
    {
        // Create Type
        MemberType newType = MemberType.MakeNew(getUser(), "Client");
        newType.Save();

        // Create Groups
        MemberGroup newGroup1 = MemberGroup.MakeNew("Client", getUser());
        MemberGroup newGroup2 = MemberGroup.MakeNew("Client1", getUser());
        MemberGroup newGroup3 = MemberGroup.MakeNew("Client2", getUser());

        newGroup1.Save();
        newGroup3.Save();
        newGroup3.Save();

        // Create Members
        CreateMember("client1.user1", new string[] { "Client", "Client1" });
        CreateMember("client1.user2", new string[] { "Client", "Client1" });
        CreateMember("client2.user1", new string[] { "Client", "Client2" });



        MemberGroup clientGroup = MemberGroup.GetByName("Client");
        MemberGroup client1Group = MemberGroup.GetByName("Client1");
        MemberGroup client2Group = MemberGroup.GetByName("Client2");

        Node clientArea = new Node(Convert.ToInt32(clientAreaId));
        Access.ProtectPage(false, clientArea.Id, Convert.ToInt32(loginPageId), Convert.ToInt32(errorPageId));
        foreach (Node childNode in clientArea.Children)
        {
            Access.ProtectPage(false, childNode.Id, Convert.ToInt32(loginPageId), Convert.ToInt32(errorPageId));
        }

        Access.AddMembershipRoleToDocument(clientArea.Id, "Client");
        Access.AddMembershipRoleToDocument(clientArea.Children[0].Id, "Client1");
        Access.AddMembershipRoleToDocument(clientArea.Children[1].Id, "Client2");

    }

    private void CreateMember(string username, string[] groups)
    {
        MemberType memType = MemberType.GetByAlias("Client");
        Member newMember = Member.MakeNew(username, username + "@test.com", memType, getUser());
        newMember.Password = "password";

        //Proceed for each new node fom supplied xmlData
        foreach (string groupAlias in groups)
        {
            MemberGroup group = MemberGroup.GetByName(groupAlias);
            newMember.AddGroup(group.Id);
        }

        newMember.Save();
    }

    private User getUser()
    {
        int id = BasePage.GetUserId(BasePage.umbracoUserContextID);
        id = (id < 0) ? 0 : id;
        return User.GetUser(id);
    }

}