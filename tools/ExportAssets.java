import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
/** Reads only the user's Minecraft jar. Run with Java 25 source-file mode. */
public class ExportAssets {
    public static void main(String[] args)throws Exception{
        if(args.length!=2)throw new IllegalArgumentException("Usage: java ExportAssets.java <client jar> <cache directory>");
        Path root=Path.of(args[1]).toAbsolutePath();Files.createDirectories(root);MessageDigest hash=MessageDigest.getInstance("SHA-256");int count=0;
        try(ZipFile zip=new ZipFile(args[0])){
            var entries=zip.stream().filter(e->!e.isDirectory()).sorted(Comparator.comparing(ZipEntry::getName)).toList();
            for(var e:entries){
                String name=e.getName();
                if(!(name.startsWith("assets/minecraft/blockstates/")&&name.endsWith(".json")||name.startsWith("assets/minecraft/models/block/")&&name.endsWith(".json")||name.startsWith("assets/minecraft/textures/block/")&&name.endsWith(".png")||name.startsWith("assets/minecraft/textures/item/")&&name.endsWith(".png")||name.startsWith("assets/minecraft/textures/entity/")&&name.endsWith(".png")||name.startsWith("assets/minecraft/models/item/")&&name.endsWith(".json")||name.startsWith("assets/minecraft/items/")&&name.endsWith(".json")||name.equals("assets/minecraft/textures/font/ascii.png")||name.startsWith("assets/minecraft/textures/gui/sprites/hud/hotbar")&&name.endsWith(".png")))continue;
                Path dest=root.resolve(name).normalize();if(!dest.startsWith(root))throw new IllegalArgumentException("Unsafe jar entry");
                byte[] data;try(var in=zip.getInputStream(e)){data=in.readAllBytes();}
                Files.createDirectories(dest.getParent());Files.write(dest,data);hash.update(name.getBytes(StandardCharsets.UTF_8));hash.update(data);count++;
            }
        }
        if(count==0)throw new IllegalArgumentException("No block assets found in client jar");
        String sha=HexFormat.of().formatHex(hash.digest());
        Files.writeString(root.resolve("manifest.json"),"{\"mcVersion\":\"26.3\",\"hash\":\""+sha+"\",\"files\":"+count+"}\n");
        System.out.println("Exported "+count+" block assets; SHA-256 "+sha+" to "+root);
    }
}
