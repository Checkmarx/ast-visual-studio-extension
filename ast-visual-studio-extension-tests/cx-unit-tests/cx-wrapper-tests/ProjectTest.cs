using ast_visual_studio_extension.CxWrapper.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class ProjectTest : BaseTest
    {
        [Fact]
        public void TestProjectShow()
        {
            List<Project> projectList = cxWrapper.GetProjects();
            Assert.True(projectList.Any());

            Project project = cxWrapper.ProjectShow(projectList[0].Id);
            Assert.Equal(project.Id, projectList[0].Id);
        }

        [Fact]
        public void TestProjectList()
        {
            List<Project> projectList = cxWrapper.GetProjects("limit=10");
            Assert.True(projectList.Count <= 10);
        }
    }
}
