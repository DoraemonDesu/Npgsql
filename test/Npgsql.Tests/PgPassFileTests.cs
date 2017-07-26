using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Npgsql.Tests
{
    [TestFixture]
    public class PgPassFileTests
    {
        [Test]
        public void ShouldParseAllEntries()
        {
            var file = PgPassFile.Load(_pgpassFile);
            var entries = file.Entries.ToList();
            Assert.That(entries.Count, Is.EqualTo(4));
        }

        [Test]
        public void ShouldFindFirstEntryWhenMultipleMatch()
        {
            var file = PgPassFile.Load(_pgpassFile);
            var entry = file.GetFirstMatchingEntry("testhost");
            Assert.That(entry.Password, Is.EqualTo("testpass"));
        }

        [Test]
        public void ShouldFindDefaultForNoMatches()
        {
            var file = PgPassFile.Load(_pgpassFile);
            var entry = file.GetFirstMatchingEntry("notarealhost");
            Assert.That(entry.Password, Is.EqualTo("defaultpass"));
        }

        [Test]
        public void CheckNullReturnType()
        {
            //Remove system default file if any
            if (File.Exists(_systemDefaultFile))
                File.Delete(_systemDefaultFile);

            //Remove app environment variable if any
            if (Environment.GetEnvironmentVariable("PGPASSFILE") != null)
                Environment.SetEnvironmentVariable("PGPASSFILE", null);

            //Assert the return type
            var file = PgPassFile.Load(null);
            Assert.That(file, Is.Null);
        }

        [Test]
        public void CheckPgPassFileReturnType()
        {
            //Assert the return type
            var file = PgPassFile.Load(_pgpassFile);
            Assert.That(file.GetType(), Is.EqualTo(typeof(PgPassFile)));
        }

        [Test]
        public void CheckPgPassFileReturnData()
        {
            var file = PgPassFile.Load(_pgpassFile);
            var entry = file.GetFirstMatchingEntry("testhost", 1235, "testwrongdatabase", "testwronguser");

            if (entry != null)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(entry.Host, Is.EqualTo("testhost"));
                    Assert.That(entry.Port, Is.EqualTo(1235));
                    Assert.That(entry.Database, Is.EqualTo("testwrongdatabase"));
                    Assert.That(entry.Username, Is.EqualTo("testwronguser"));
                    Assert.That(entry.Password, Is.EqualTo("testwrongpass"));
                });
            }
        }

        [Test]
        public void ReproduceErrorWithForbidenCharacters()
        {
            //Injecting raw path
            var file = PgPassFile.Load(_appDataPath + "\\postgresql<>:\"/\\|?*\\pgpass.conf");
            Assert.That(file, Is.Null);
        }

        [Test]
        public void ReproduceErrorWithReservedNames()
        {
            //Injecting raw path
            var file = PgPassFile.Load(_appDataPath + "\\COM1\\pgpass.conf");
            Assert.That(file, Is.Null);
        }

        [Test]
        public void CheckSystemDefaultPath()
        {
            //Set file under system-dependent path
            try
            {
                Directory.CreateDirectory(_systemDefaultPath);
                using (var fs = File.Create(_systemDefaultFile)) { }

                File.WriteAllText(_systemDefaultFile, _content);
                _systemDefaultSet = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //Assert if returned file is Npgsql.PgPassFile
            if (_systemDefaultSet)
            {
                var file = PgPassFile.Load(null);
                Assert.That(file.GetType(), Is.EqualTo(typeof(PgPassFile)));
            }

            //Clean up
            if (File.Exists(_systemDefaultFile))
            {
                File.Delete(_systemDefaultFile);
                Directory.Delete(_systemDefaultPath);
            }
        }

        [Test]
        public void CheckEnvironmentVariablePath()
        {
            //Set "PGPASSFILE" app environment variable
            try
            {
                Environment.SetEnvironmentVariable("PGPASSFILE", _pgpassFile);
                _environmentVariableSet = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //Assert if returned type is Npgsql.PgPassFile
            if (_environmentVariableSet)
            {
                var file = PgPassFile.Load(null);
                Assert.That(file.GetType(), Is.EqualTo(typeof(PgPassFile)));
            }

            //Clean up
            Environment.SetEnvironmentVariable("PGPASSFILE", null);
        }

        string _content, _pgpassFile, _appDataPath, _systemDefaultPath, _systemDefaultFile;
        bool _systemDefaultSet = false, _environmentVariableSet = false;

        [OneTimeSetUp]
        public void CreateTestFile()
        {
            //set up pgpass file with fake content that can be used for this test
            _content = @"testhost:1234:testdatabase:testuser:testpass
testhost:1235:testwrongdatabase:testwronguser:testwrongpass
testhost:*:*:*:testdefaultpass
# helpful comment goes here
*:*:*:*:defaultpass";

            _pgpassFile = Path.GetTempFileName();
            _appDataPath = Environment.GetEnvironmentVariable("APPDATA");
            _systemDefaultPath = Path.Combine(_appDataPath, "postgresql");
            _systemDefaultFile = Path.Combine(_systemDefaultPath, "pgpass.conf");

            File.WriteAllText(_pgpassFile, _content);
        }

        [OneTimeTearDown]
        public void DeleteTestFile()
        {
            if (File.Exists(_pgpassFile))
                File.Delete(_pgpassFile);
        }
    }
}