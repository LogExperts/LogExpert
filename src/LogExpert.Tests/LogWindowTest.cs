using NUnit.Framework;

namespace LogExpert.Tests;

//TODO Find out why there is a "Drag and Drop Exception" until then, this 2 Tests can not be executed the block the build pipeline
[TestFixture]
public class LogWindowTest
{
    //// TODO: Add more tests when DI container is ready.
    //[TestCase(@".\TestData\JsonColumnizerTest_01.txt", typeof(DefaultLogfileColumnizer))]
    //public void Instantiate_JsonFile_BuildCorrectColumnizer(string fileName, Type columnizerType)
    //{
    //    LogTabWindow logTabWindow = new(null, 0, false);
    //    LogWindow logWindow = new(logTabWindow, fileName, false, false);

    //    Assert.That(columnizerType, Is.EqualTo(logWindow.CurrentColumnizer.GetType()));
    //}

    //[TestCase(@".\TestData\XmlTest_01.xml")]
    //[TestCase(@".\TestData\CsvTest_01.csv")]
    //public void Instantiate_AnyFile_NotCrash(string fileName)
    //{
    //    PluginRegistry.GetInstance().RegisteredColumnizers.Add(new Log4jXmlColumnizer());
    //    PluginRegistry.GetInstance().RegisteredColumnizers.Add(new CsvColumnizerType());

    //    LogTabWindow logTabWindow = new(null, 0, false);
    //    LogWindow logWindow = new(logTabWindow, fileName, false, false);

    //    Assert.That(true, Is.True);
    //}
}
