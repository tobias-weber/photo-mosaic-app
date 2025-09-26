namespace backend.Helpers;

public static class FileHelpers
{
    public static bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension is ".jpg" or ".jpeg" or ".png";
    }
    
}