namespace PoePriceRunesOfAldurHelperRu;

public class OcrResultInfo
{
    public List<LineInfo> Lines { get; set; } = [];
}

public class LineInfo
{
    public required string Text { get; set; }
    public int Y { get; set; }
    public int Height { get; set; }
}
