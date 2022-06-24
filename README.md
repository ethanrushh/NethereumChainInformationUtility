# NethereumChainInformationUtility

A tool to help track down the chains and RPCs that do/don't support EIP-1559 transactions for the better of Nethereum.

## Requirements:
.NET 6 Runtime/SDK

## To Run:
`dotnet run`

## Output:
Exit code will be -1 if a crash detected, else 0 for success. Console output will be a JSON pretty-formatted string with properties. It is a list of objects with the format:

```
  {
    "ChainId": <int>,
    "Rpc": <string>,
    "HasEIP1559Support": <bool>
  }
```
