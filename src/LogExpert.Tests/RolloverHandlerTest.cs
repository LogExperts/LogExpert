﻿using LogExpert.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.PluginRegistry.FileSystem;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace LogExpert.Tests
{
    [TestFixture]
    internal class RolloverHandlerTest : RolloverHandlerTestBase
    {
        [Test]
        [TestCase("*$J(.)", 66)]
        public void TestFilenameListWithAppendedIndex(string format, int retries)
        {
            MultiFileOptions options = new();
            options.FormatPattern = format;
            options.MaxDayTry = retries;

            LinkedList<string> files = CreateTestFilesWithoutDate();

            string firstFile = files.Last.Value;

            ILogFileInfo info = new LogFileInfo(new Uri(firstFile));
            RolloverFilenameHandler handler = new(info, options);
            LinkedList<string> fileList = handler.GetNameList();

            Assert.That(fileList, Is.EqualTo(files));

            Cleanup();
        }

        [Test]
        [TestCase("*$D(YYYY-mm-DD)_$I.log", 3)]
        public void TestFilenameListWithDate(string format, int retries)
        {
            MultiFileOptions options = new();
            options.FormatPattern = format;
            options.MaxDayTry = retries;

            LinkedList<string> files = CreateTestFilesWithDate();

            string firstFile = files.Last.Value;

            ILogFileInfo info = new LogFileInfo(new Uri(firstFile));
            RolloverFilenameHandler handler = new(info, options);
            LinkedList<string> fileList = handler.GetNameList();

            Assert.That(fileList, Is.EqualTo(files));

            Cleanup();
        }
    }
}