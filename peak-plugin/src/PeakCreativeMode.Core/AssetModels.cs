using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
    /// Resolves Minecraft model parents and #texture references without shipping any assets.
    public sealed class AssetModels
    {
        private readonly string _root;private readonly Func<string,string> _loader;
        public AssetModels(Func<string,string> loader){_loader=loader??throw new ArgumentNullException(nameof(loader));}
        private JObject Read(string id,string folder)=>JObject.Parse(_loader!=null?_loader(PathFor(id,folder,".json").Replace(System.IO.Path.DirectorySeparatorChar,'/')):File.ReadAllText(PathFor(id,folder,".json")));
        public AssetModels(string root)
        {
            _root=root;var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")));
            if((string)manifest["mcVersion"]!="26.3")throw new InvalidDataException("Export the Minecraft 26.3 block cache first.");
        }
        private string PathFor(string id,string folder,string suffix)
        {
            if(!id.Contains(":"))id="minecraft:"+id;
            string[] parts=id.Split(':');if(parts.Length!=2||parts.Any(p=>p.Contains("..")||p.Contains("\\")))throw new InvalidDataException("Invalid asset id");
            return Path.Combine(_root??"","assets",parts[0],folder,parts[1]+suffix);
        }
        public string TexturePath(string id)=>PathFor(id,"textures",".png");
        private JObject Model(string id,int depth=0)
        {
            if(depth>32)throw new InvalidDataException("Cyclic model parent");
            var child=Read(id,"models");
            var result=child["parent"] is JValue parent?Model((string)parent,depth+1):new JObject();
            var textures=(JObject)(result["textures"]??new JObject());
            if(child["textures"] is JObject own)foreach(var pair in own)textures[pair.Key]=pair.Value.DeepClone();
            result["textures"]=textures;
            if(child["elements"]!=null)result["elements"]=child["elements"].DeepClone();
            return result;
        }
        public List<ModelQuad> ItemQuads(string model)=>BlockModelGeometry.Build(Model(model));
        public List<ModelQuad> Quads(string state)
        {
            string block=state.Split('[')[0];var states=Read(block,"blockstates");
            var output=new List<ModelQuad>();
            void Add(JToken v){if(v is JArray a)v=a[0];var o=(JObject)v;output.AddRange(BlockModelGeometry.Build(Model((string)o["model"]),(float?)o["x"]??0,(float?)o["y"]??0));}
            if(states["variants"] is JObject variants)Add(BlockModelGeometry.Variant(variants,state));
            if(states["multipart"] is JArray parts)foreach(JObject part in parts)if(BlockModelGeometry.Matches(part["when"],state))Add(part["apply"]);
            return output;
        }

        public Dictionary<string,string> Faces(string block)
        {
            var states=Read(block,"blockstates");
            var variants=states["variants"] as JObject;
            JToken variant=variants?[""]??variants?["axis=y"]??variants?.Properties().FirstOrDefault()?.Value;
            if(variant is JArray array)variant=array[0];
            string model=(string)(variant?["model"]);if(model==null)throw new InvalidDataException("Phase 2 requires a cube variant: "+block);
            var resolved=Model(model);var textures=(JObject)resolved["textures"];
            var faces=resolved["elements"]?[0]?["faces"] as JObject;
            if(faces==null)throw new InvalidDataException("No model faces for "+block);
            var result=new Dictionary<string,string>();
            foreach(var face in faces)
            {
                string value=(string)face.Value["texture"];int n=0;
                while(value.StartsWith("#",StringComparison.Ordinal))
                {
                    if(++n>32)throw new InvalidDataException("Cyclic texture reference");
                    value=(string)textures[value.Substring(1)]??throw new InvalidDataException("Missing texture reference");
                }
                result[face.Key]=value.Contains(":")?value:"minecraft:"+value;
            }
            return result;
        }
    }
}
