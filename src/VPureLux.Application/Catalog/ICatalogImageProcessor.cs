namespace VPureLux.Catalog;

public interface ICatalogImageProcessor
{
    ImageData Process(string imageBase64, string mimeType, string fileName);

    byte[] CreateThumbnail(ImageData image);
}
