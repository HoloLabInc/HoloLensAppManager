
using System;
using HoloLensAppManager.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static HoloLensAppManager.Helpers.SupportedArchitectureHelper;
namespace UnitTestProject
{
    [TestClass]
    public class SupportedArchitectureHelperTest
    {
        [TestMethod]
        public void TestGetSupportedArchitectureFromAppPackage()
        {
            {
                var archi = GetSupportedArchitectureFromAppPackage("MRTK Examples Hub_2.1.0.0_arm_Master.appxbundle");
                Assert.AreEqual(SupportedArchitectureType.Arm, archi);
            }
            {
                var archi = GetSupportedArchitectureFromAppPackage("MRTK_Examples_Hub_2.1.0.0_arm_arm64_Master.appxbundle");
                Assert.AreEqual(SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64, archi);
            }
            {
                var archi = GetSupportedArchitectureFromAppPackage("DummyApp_1.0.0.0_x86_x64_arm_Debug.msixbundle");
                Assert.AreEqual(SupportedArchitectureType.Arm | SupportedArchitectureType.X86 | SupportedArchitectureType.X64, archi);
            }
            {
                var archi = GetSupportedArchitectureFromAppPackage("DummyApp_1.0.1.0_x86_x64_arm64.msixbundle");
                Assert.AreEqual(SupportedArchitectureType.X64 | SupportedArchitectureType.X86 | SupportedArchitectureType.Arm64, archi);
            }
        }
    }
}
