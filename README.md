
# MiniJSON

A C# JSON parser written in only 600 lines

## Example:
```CSharp

// Parse JSON from a string
JSONNode node = JSONNode.Parse(json);

            
// Print some values
Console.WriteLine("String: " + node.GetString("string"));
Console.WriteLine("Bool: " + node.GetBool("boolean"));
Console.WriteLine("Node 2 Value: " + node.GetNode("nodes")?.GetNode("node2")?.GetFloat("value"));


// Increment number
int num = node.GetInt("number");
node.SetInt("number", num + 1);

// Change string
node.SetString("string", $"value_{num}");

// Flip boolean
node.SetBool("boolean", !node.GetBool("boolean"));
            
// Save to output.json
File.WriteAllText("output.json", node.Print());
```

Using the following JSON
```JSON
{
    "string":"value",
    "boolean":true,
    "number":1,
    "array":["A","B","C","D","E"],
    "2d array": [[1,2],[3,4]],
    "3d array": [[[1,2],[3,4]],[[2,3],[4,5]]],
    "nodes":
    {
        "node1":
        {
            "value":1
        },
        "node2":
        {
            "value":1.5
        }
    }
}
```
