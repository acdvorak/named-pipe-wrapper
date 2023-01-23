using Nuke.Common;
using Nuke.Common.Execution;
using ricaun.Nuke;
using ricaun.Nuke.Components;

internal class Build : NukeBuild, IPublishPack, ITest
{
     string ITest.TestProjectName => "UnitTests";
    public static int Main() => Execute<Build>(x => x.From<IPublishPack>().Build);
}