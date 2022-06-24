namespace NethereumChainInformationUtility.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSuccessfulExitCode()
        {
            Assert.IsTrue(Task.Run(Task<int>? () => ChainInformationUtility.Program.Main(Array.Empty<string>())).Result == 0);
        }
    }
}