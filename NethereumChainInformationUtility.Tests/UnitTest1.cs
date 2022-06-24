namespace NethereumChainInformationUtility.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSuccessfulExitCode()
        {
            Assert.IsTrue(Task.Run<int>(Task<int> () => ChainInformationUtility.Program.Main(new string[] { })).Result == 0);
        }
    }
}