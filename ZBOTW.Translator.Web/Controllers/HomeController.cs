using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using ZBOTW.Translator.Web.Models;

namespace ZBOTW.Translator.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Thai),
            WriteIndented = true
        };

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        private void ConvertMsytToJson()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Original", "EventFlowMsg"));
            
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);
                
                var myst = new MessageTable(fileName);
                MessageEntry entry = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {
                        
                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entry = new MessageEntry(name);
                        myst.EntryList.Add(entry);
                    }
                    if (lines[i].Trim().StartsWith("- text:"))
                    {
                       
                        var trim = unescapedDoublequoteRegex.Replace(lines[i].Trim().Replace("- text: ", ""),"").Replace("\\\"","\"");
                        trim=trim.Replace("\\n", "\n");
                        var text = new MessageText() { Line = i + 1, OriginalText = trim };
                        //text.TranslatedText = text.OriginalText;
                        entry.TextList.Add(text);
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(myst, new JsonSerializerOptions() { WriteIndented = true }));
            }
        }

        private void ExecMergeFromTranslatedMsyt()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Translated", "EventFlowMsg"));

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);

                var mstb = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json","EventFlowMsg",$"{fileName}.json")));
                if (mstb == null) throw new InvalidOperationException("Null MessageTable");
                var entryName = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {

                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entryName = name;
                    }
                    if (lines[i].Trim().StartsWith("- text:"))
                    {

                        var trim = unescapedDoublequoteRegex.Replace(lines[i].Trim().Replace("- text: ", ""), "").Replace("\\\"", "\"");
                        trim = trim.Replace("\\n", "\n");
                        var text = new MessageText() { Line = i + 1, OriginalText = trim };
                        var entry = mstb.EntryList.FirstOrDefault(c => c.EntryName == entryName);
                        if (entry == null) throw new InvalidOperationException($"{fileName} Entry {entryName} Not Found");
                        var messageText = entry.TextList.FirstOrDefault(c => c.Line == i + 1);
                        if (messageText == null) throw new InvalidOperationException($"{fileName} Text Line {i+1} Not Found");
                        messageText.TranslatedText = trim;
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(mstb, jsonSerializerOptions));
            }
        }
        private FileStreamResult ExportMsyt()
        {
            
            var textValueRegex = new Regex("text: .+");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Json", "EventFlowMsg"));
            var exportDir = System.IO.Directory.CreateDirectory(Path.Combine("Msyt", "Export", "EventFlowMsg"));
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(file.FullName));
                var msyt = System.IO.File.ReadAllLines(Path.Combine("Msyt", "Original", "EventFlowMsg",$"{Path.GetFileNameWithoutExtension(file.Name)}.msyt"));
                foreach(var entry in json.EntryList)
                {
                    foreach(var item in entry.TextList)
                    {
                        var t = msyt[item.Line - 1];
                        msyt[item.Line - 1] = textValueRegex.Replace(t, "text: " + JsonSerializer.Serialize(item.TranslatedText,jsonSerializerOptions));
                    }
                }
                System.IO.File.WriteAllLines(Path.Combine("Msyt", "Export", "EventFlowMsg", $"{Path.GetFileNameWithoutExtension(file.Name)}.msyt"), msyt);
            }

            var outStream = new MemoryStream();
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    files = exportDir.GetFiles();
                    foreach (var file in files)
                    {
                        var filename = file.Name;
                        var fileInArchive = archive.CreateEntry(filename, CompressionLevel.Optimal);
                        using (var entryStream = fileInArchive.Open())
                        using (var fileToCompressStream = new FileStream(file.FullName, FileMode.Open))
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }
                }
                outStream.Seek(0, SeekOrigin.Begin);
                return File(outStream, System.Net.Mime.MediaTypeNames.Application.Zip, $"EventFlowMsg.zip");
            }
        }

        public IActionResult Index()
        {
            //TestRead();
            return View("Translate");
        }

        [Route("ConvertMsytToJson")]
        public IActionResult ConvertMsytToJsonAction()
        {
            ConvertMsytToJson();
            return RedirectToAction("Index");
        }

        public IActionResult MergeFromTranslatedMsyt()
        {
            ExecMergeFromTranslatedMsyt();
            return RedirectToAction("Index");
        }

        public IActionResult ExportTranslatedMsyt()
        {
            return ExportMsyt();
        }

        public IActionResult ExportJson()
        {
            var outStream = new MemoryStream();
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    var dir = new System.IO.DirectoryInfo(Path.Combine("Json", "EventFlowMsg"));
                    var files = dir.GetFiles();
                    foreach (var file in files)
                    {
                        var filename = file.Name;
                        var fileInArchive = archive.CreateEntry(filename, CompressionLevel.Optimal);
                        using (var entryStream = fileInArchive.Open())
                        using (var fileToCompressStream = new FileStream(file.FullName, FileMode.Open))
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }
                }
                outStream.Seek(0, SeekOrigin.Begin);
                return File(outStream, System.Net.Mime.MediaTypeNames.Application.Zip, $"EventFlowMsgJson.zip");
            }
        }

        public IActionResult GetFileList()
        {
            var dir = new System.IO.DirectoryInfo(Path.Combine("Json", "EventFlowMsg"));
            var files = dir.GetFiles();
            return Json(files.Select(s => Path.GetFileNameWithoutExtension(s.Name)));
        }
        public IActionResult GetMessageTable(string fileName)
        {
            var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));
            return Json(json);
        }

        public IActionResult Edit()
        {
            var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(@"Json\EventFlowMsg\Animal_Forest.json"));
            return View(json);
        }

        public IActionResult Translate()
        {            
            return View();
        }

        public IActionResult Save([FromBody]MessageTable messageTable)
        {
            System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{messageTable.FileName}.json"), JsonSerializer.Serialize(messageTable, jsonSerializerOptions));
            return Json(1);
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}