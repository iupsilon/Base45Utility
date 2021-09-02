# Base45Utility
Base45 encoding and decoding utility for .Net 

## Encoding usage (1)
```c#
var base45 = new Base45();
var messageText = "Hello world";
var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageText);
var b45Encoded = base45.Encode(messageBytes);

## Encoding usage (2)
```c#
var base45 = new Base45();
var messageText = "Hello world";
var b45Encoded = base45.Encode(messageText);
```

## Decoding usage
```c#
var base45 = new Base45();
var base45Encoded = "%69 VD82EK4F.KEA2";
var base45DecodedAsBytes = base45.Decode(base45Encoded);
var base45DecodedAsString = base45.DecodeAsString(base45Encoded);
```
