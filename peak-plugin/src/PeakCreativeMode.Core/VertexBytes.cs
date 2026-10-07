using System;
namespace PeakCreativeMode.Core
{
 public static class VertexBytes
 {
  public static int Size(string format){switch(format){case "Float32":case "UInt32":case "SInt32":return 4;case "Float16":case "UNorm16":case "SNorm16":case "UInt16":case "SInt16":return 2;case "UNorm8":case "SNorm8":case "UInt8":case "SInt8":return 1;default:throw new ArgumentException("Unsupported vertex format "+format);}}
  public static float Read(byte[] data,int offset,string format)
  {
   switch(format){case "Float32":return BitConverter.ToSingle(data,offset);case "Float16":return Half(BitConverter.ToUInt16(data,offset));case "UNorm8":return data[offset]/255f;case "SNorm8":return Math.Max(-1,unchecked((sbyte)data[offset])/127f);case "UNorm16":return BitConverter.ToUInt16(data,offset)/65535f;case "SNorm16":return Math.Max(-1,BitConverter.ToInt16(data,offset)/32767f);case "UInt8":return data[offset];case "SInt8":return unchecked((sbyte)data[offset]);case "UInt16":return BitConverter.ToUInt16(data,offset);case "SInt16":return BitConverter.ToInt16(data,offset);case "UInt32":return BitConverter.ToUInt32(data,offset);case "SInt32":return BitConverter.ToInt32(data,offset);default:throw new ArgumentException("Unsupported vertex format "+format);}
  }
  private static float Half(ushort bits){int exponent=(bits>>10)&31,mantissa=bits&1023;float sign=(bits&32768)==0?1:-1;if(exponent==0)return sign*mantissa*(float)Math.Pow(2,-24);if(exponent==31)return mantissa==0?sign*float.PositiveInfinity:float.NaN;return sign*(1+mantissa/1024f)*(float)Math.Pow(2,exponent-15);}
 }
}
