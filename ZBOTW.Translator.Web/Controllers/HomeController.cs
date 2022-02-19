using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),//(UnicodeRanges.BasicLatin, UnicodeRanges.Thai,UnicodeRanges.GeometricShapes,UnicodeRanges.MiscellaneousSymbols),
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

        private void ExecUpdateColour()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Original", "EventFlowMsg"));

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);

                var mstb = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));
                if (mstb == null) throw new InvalidOperationException("Null MessageTable");
                var entryName = "";
                string? colour = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {

                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entryName = name;
                        colour = null;
                    }
                    if (lines[i].Trim().StartsWith("kind: reset_colour")) {
                        colour = null;
                    }
                    if (lines[i].Trim().StartsWith("colour: "))
                    {
                        colour = lines[i].Replace("colour:", "").Trim();
                    }
                    if (lines[i].Trim().StartsWith("- text:"))
                    {
                        //var trim = unescapedDoublequoteRegex.Replace(lines[i].Trim().Replace("- text: ", ""), "").Replace("\\\"", "\"");
                        //trim = trim.Replace("\\n", "\n");
                        var entry = mstb.EntryList.FirstOrDefault(c => c.EntryName == entryName);
                        if (entry == null) throw new InvalidOperationException($"{fileName} Entry {entryName} Not Found");
                        var messageText = entry.TextList.FirstOrDefault(c => c.Line == i + 1);
                        if (messageText == null) throw new InvalidOperationException($"{fileName} Text Line {i + 1} Not Found");
                        messageText.Colour = colour;
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(mstb, jsonSerializerOptions));
            }
        }

        private void ExecUpdateNPC()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Original", "EventFlowMsg"));
            var NpcJson = JObject.Parse(System.IO.File.ReadAllText(Path.Combine("Json", "NPC.json")));
            var entries = NpcJson["entries"] as JObject;
            if (entries == null) return;
            var NpcDic = (from key in entries.Properties().Select(s => s.Name)
                          select new { Key = key.Replace("_Name",""), Value = entries[key]["text"].Value<string>() }).ToDictionary(x=>x.Key,x=>x.Value);
                         
            
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);

                var mstb = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));               
                if (mstb == null) throw new InvalidOperationException("Null MessageTable");
                var entryName = "";
                string? npc = null;
                string? npcname = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {

                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entryName = name;
                    }
                    
                    if (lines[i].Trim().StartsWith("attributes: "))
                    {
                        var temp = lines[i].Replace("attributes:", "").Trim().Replace("\"", "");
                        if (!string.IsNullOrEmpty(temp))
                        {
                            if (npc != temp)
                            {
                                npc = temp;
                                npcname = null;
                                NpcDic.TryGetValue(npc, out npcname);
                            }
                            if(npcname != null)
                            {
                                var entry = mstb.EntryList.FirstOrDefault(c => c.EntryName == entryName);
                                if (entry == null) throw new InvalidOperationException($"{fileName} Entry {entryName} Not Found");
                                entry.NPC = npcname;
                            }
                        }
                    }                    
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(mstb, jsonSerializerOptions));
            }
        }

        private void ExecUpdateVariable()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Original", "EventFlowMsg"));

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);

                var mstb = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));
                if (mstb == null) throw new InvalidOperationException("Null MessageTable");
                var entryName = "";
                bool foundVariable = false;
                string? variable = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {

                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entryName = name;
                        foundVariable = false;
                        variable = null;
                    }
                    if (lines[i].Trim().StartsWith("kind: variable"))
                    {
                        foundVariable = true;
                    }
                    if (foundVariable && lines[i].Trim().StartsWith("name: "))
                    {
                        variable = lines[i].Replace("\"", "").Replace("name: ", "").Trim();
                    }
                    if (lines[i].Trim().StartsWith("- text:"))
                    {
                        var entry = mstb.EntryList.FirstOrDefault(c => c.EntryName == entryName);
                        if (entry == null) throw new InvalidOperationException($"{fileName} Entry {entryName} Not Found");
                        var messageText = entry.TextList.FirstOrDefault(c => c.Line == i + 1);
                        if (messageText == null) throw new InvalidOperationException($"{fileName} Text Line {i + 1} Not Found");
                        messageText.Variable = variable;
                        foundVariable = false;
                        variable = null;
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(mstb, jsonSerializerOptions));
            }
        }

        private void ExecUpdateChoice()
        {
            var entryNameRegex = new Regex("^  [a-zA-Z0-9\"]");
            var unescapedDoublequoteRegex = new Regex("(?<!\\\\)\"");
            var dir = new System.IO.DirectoryInfo(Path.Combine("Msyt", "Original", "EventFlowMsg"));

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var lines = System.IO.File.ReadAllLines(file.FullName);

                var mstb = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));
                if (mstb == null) throw new InvalidOperationException("Null MessageTable");
                var entryName = "";
                bool foundChoice = false;
                List<int> choices = new List<int>();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (entryNameRegex.Match(lines[i]).Success)
                    {

                        var name = lines[i].Replace("\"", "").Replace(":", "").Trim();
                        entryName = name;
                        foundChoice = false;
                    }

                    if (lines[i].Trim().StartsWith("choice_labels:"))
                    {
                        foundChoice = true;
                        choices = new List<int>();
                        continue;
                    }
                    if(foundChoice)
                    {
                        if (lines[i].Trim().StartsWith("-"))
                        {
                            var temp = lines[i].Replace("-", "").Trim();
                            if (!string.IsNullOrEmpty(temp))
                            {
                                choices.Add(Int32.Parse(temp));                                
                            }
                        }
                        else
                        {
                            var entry = mstb.EntryList.FirstOrDefault(c => c.EntryName == entryName);
                            if (entry == null) throw new InvalidOperationException($"{fileName} Entry {entryName} Not Found");
                            entry.Choices = choices.ToArray();
                            foundChoice = false;
                        }
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(mstb, jsonSerializerOptions));
            }
        }

        private void ExecUpdateTextSize()
        {
            var dir = new System.IO.DirectoryInfo(Path.Combine("Json", "EventFlowMsg"));
            var exportDir = System.IO.Directory.CreateDirectory(Path.Combine("Msyt", "Export", "EventFlowMsg"));
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(file.FullName));
                foreach (var entry in json.EntryList)
                {
                    foreach (var text in entry.TextList)
                    {
                        text.Length = text.OriginalText?.Length ?? 0;
                    }
                }
                System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json"), JsonSerializer.Serialize(json, jsonSerializerOptions));
            }
        }

        private FileStreamResult ExportMsyt()
        {
            var encoderSettings = new TextEncoderSettings();
            encoderSettings.AllowCharacters('\u0027');
            encoderSettings.AllowRange(UnicodeRanges.BasicLatin);
            
            JsonSerializerOptions msytSerializerOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(encoderSettings)

            };

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
                        msyt[item.Line - 1] = textValueRegex.Replace(t, "text: " + JsonSerializer.Serialize(item.TranslatedText,msytSerializerOptions));
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

        public IActionResult UpdateJson()
        {
            //ExecUpdateColour();
            //ExecUpdateNPC();
            //ExecUpdateVariable();
            //ExecUpdateChoice();
            ExecUpdateTextSize();
            return RedirectToAction("Index");
        }

        public IActionResult GenSummaryFile()
        {
            var summary = new List<MessageTableInfo>();
            var dir = new System.IO.DirectoryInfo(Path.Combine("Json", "EventFlowMsg"));
            var exportDir = System.IO.Directory.CreateDirectory(Path.Combine("Msyt", "Export", "EventFlowMsg"));
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(file.FullName));
                var text = json.EntryList.Sum(s => s.TextList.Count());
                var translatedText = json.EntryList.Sum(s => s.TextList.Where(c => !string.IsNullOrEmpty(c.TranslatedText)).Count());
                var info = new MessageTableInfo()
                {
                    FileName = json.FileName,
                    IsCompleted = json.IsCompleted || text==translatedText,
                    Entry = json.EntryList.Count(),
                    Text = text,
                    TranslatedText = translatedText,
                    Length = json.EntryList.Sum(s => s.TextList.Sum(ss=>ss.Length)),
                    TranslatedLength = json.EntryList.Sum(s => s.TextList.Where(c => !string.IsNullOrEmpty(c.TranslatedText)).Sum(ss => ss.Length))
                };
                summary.Add(info);
            }
            System.IO.File.WriteAllText(Path.Combine("Json", $"EventFlowMsg.json"), JsonSerializer.Serialize(summary, jsonSerializerOptions));
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

        private List<MessageTableInfo> GetSummary()
        {
            return JsonSerializer.Deserialize<List<MessageTableInfo>>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg.json")));
        }
        private List<MessageTableInfo> SaveSummary(MessageTable messageTable)
        {
            var summary = GetSummary();
            var info = summary.Find(c => c.FileName.Equals(messageTable.FileName));
            info.TranslatedText = messageTable.EntryList.Sum(s => s.TextList.Where(c => !string.IsNullOrEmpty(c.TranslatedText)).Count());
            info.TranslatedLength = messageTable.EntryList.Sum(s => s.TextList.Where(c => !string.IsNullOrEmpty(c.TranslatedText)).Sum(ss => ss.Length));
            info.IsCompleted = messageTable.IsCompleted || info.Text == info.TranslatedText;
            System.IO.File.WriteAllText(Path.Combine("Json", $"EventFlowMsg.json"), JsonSerializer.Serialize(summary, jsonSerializerOptions));
            return summary;
        }

        public IActionResult GetFileList()
        {
            var summary = GetSummary();
            var progress = new ProgressViewModel(summary);
            return Json(new { files= summary, progress});
        }
        public IActionResult GetMessageTable(string fileName)
        {
            var json = JsonSerializer.Deserialize<MessageTable>(System.IO.File.ReadAllText(Path.Combine("Json", "EventFlowMsg", $"{fileName}.json")));
            return Json(json);
        }

        public IActionResult Translate()
        {            
            return View();
        }
               

        public IActionResult Save([FromBody]MessageTable messageTable)
        {
            System.IO.File.WriteAllText(Path.Combine("Json", "EventFlowMsg", $"{messageTable.FileName}.json"), JsonSerializer.Serialize(messageTable, jsonSerializerOptions));
            var summary = SaveSummary(messageTable);
            var progress = new ProgressViewModel(summary);
            var currentTranslatedText = summary.Find(c => c.FileName == messageTable.FileName)?.TranslatedText ?? 0;
            return Json(new { progress, currentTranslatedText});
        }
        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}