using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Permaverse.AO
{
	[Serializable]
	public class Results
	{
		public List<Edge> Edges;
		//public bool hasNextPage;

		public Results(string jsonString)
		{
			Edges = new List<Edge>();

			var jsonNode = JSON.Parse(jsonString);

			Edges = new List<Edge>();
			if (jsonNode.HasKey("edges"))
			{
				foreach (var edgeNode in jsonNode["edges"].AsArray)
				{
					Edges.Add(new Edge(edgeNode));
				}
			}
		}

		public List<string> GetAllData(string key)
		{
			List<string> data = new List<string>();

			foreach (Edge edge in Edges)
			{
				if (edge.Node != null && edge.Node is NodeCU networkResponse && networkResponse.Messages.Count > 0)
				{
					foreach (Message message in networkResponse.Messages)
					{
						if (!string.IsNullOrEmpty(message.Data))
						{
							JSONNode dataNode = JSON.Parse(message.Data);

							if (dataNode.HasKey(key))
							{
								data.Add(message.Data);
							}
						}
					}
				}
			}

			return data;
		}
	}

	[Serializable]
	public class Edge
	{
		public Node Node;
		public string Cursor;

		public Edge(JSONNode edgeNode)
		{
			if (edgeNode.HasKey("node"))
			{
				// Determine the type of node based on content and instantiate accordingly
				if (edgeNode["node"].HasKey("message"))
				{
					Node = new NodeSU(edgeNode["node"]);
				}
				else if (edgeNode["node"].HasKey("Messages"))
				{
					Node = new NodeCU(edgeNode["node"]);
				}
				else
				{
					Debug.LogError("NO KEY MESSAGES!!");
				}
			}

			if (edgeNode.HasKey("cursor"))
			{
				Cursor = edgeNode["cursor"];
			}
		}
	}

	[Serializable]
	public abstract class Node
	{

	}

	[Serializable]
	public class NodeSU : Node
	{
		public MessageSU Message { get; set; }
		//public string Assignment { get; set; }

		public NodeSU(JSONNode node)
		{
			if (node.HasKey("message"))
			{
				Message = new MessageSU(node["message"]);
			}

			//if(node.HasKey("assignment"))
			//{
			//	Assignment = node["assignment"];
			//}
		}
	}

	[Serializable]
	public class NodeCU : Node
	{
		public List<Message> Messages { get; set; }
		public List<string> Assignments { get; set; }
		public List<string> Spawns { get; set; }
		public Output Output { get; set; }
		public string Error { get; set; }
		public long GasUsed { get; set; }

		public NodeCU(string jsonString) : this(JSON.Parse(jsonString))
		{
		}

		public NodeCU(JSONNode jsonNode)
		{
			Messages = new List<Message>();
			if (jsonNode.HasKey("Messages"))
			{
				foreach (var messageNode in jsonNode["Messages"].AsArray)
				{
					Messages.Add(new Message(messageNode));
				}
			}

			Assignments = new List<string>();
			if (jsonNode.HasKey("Assignments"))
			{
				foreach (JSONNode assignmentNode in jsonNode["Assignments"].AsArray)
				{
					Assignments.Add(assignmentNode);
				}
			}

			Spawns = new List<string>();
			if (jsonNode.HasKey("Spawns"))
			{
				foreach (JSONNode spawnNode in jsonNode["Spawns"].AsArray)
				{
					Spawns.Add(spawnNode);
				}
			}

			if (jsonNode.HasKey("Output"))
			{
				JSONNode outputObj = jsonNode["Output"];
				Output = new Output();

				if (outputObj.HasKey("data"))
				{
					Output.Data = outputObj["data"];
				}

				if (outputObj.HasKey("print"))
				{
					Output.Print = outputObj["print"].AsBool;
				}

				if (outputObj.HasKey("prompt"))
				{
					Output.Prompt = outputObj["prompt"];
				}
			}

			Error = jsonNode.HasKey("Error") ? jsonNode["Error"] : null;
			GasUsed = jsonNode.HasKey("GasUsed") ? jsonNode["GasUsed"].AsLong : 0;
		}

		public bool IsSuccessful()
		{
			return string.IsNullOrEmpty(Error);
		}

		public override string ToString()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("Result:");

			if (Messages != null && Messages.Count > 0)
			{
				sb.AppendLine($"Messages Count: {Messages.Count}\n");

				int i = 0;
				foreach (Message m in Messages)
				{
					// Print Message if present
					if (m != null)
					{
						// Print Data if not empty
						if (!string.IsNullOrEmpty(m.Data))
						{
							sb.AppendLine($"Message[{i}] Data: {m.Data}");
						}
						// Print Tags if any
						if (m.Tags != null && m.Tags.Count > 0)
						{
							foreach (Tag t in m.Tags)
							{
								sb.AppendLine($"Message[{i}] Tag: {t.Name} = {t.Value}");
							}
						}
					}

					sb.AppendLine();
					i++;
				}
			}

			// Print Error if not empty
			if (!string.IsNullOrEmpty(Error))
			{
				sb.AppendLine($"Error: {Error}");
			}

			// Print Spawns if available
			int spawnsCount = Spawns?.Count ?? 0;
			if (spawnsCount > 0)
			{
				sb.AppendLine($"Spawns Count: {spawnsCount}");
			}

			// Print Output if not empty
			if (Output != null)
			{
				if (!string.IsNullOrEmpty(Output.Data))
				{
					sb.AppendLine($"Output.Data: {Output.Data}");
				}
				if (Output.Print)
				{
					sb.AppendLine($"Output.Print: {Output.Print}");
				}
				if (!string.IsNullOrEmpty(Output.Prompt))
				{
					sb.AppendLine($"Output.Prompt: {Output.Prompt}");
				}
			}

			// Print GasUsed if non-zero
			if (GasUsed != 0)
			{
				sb.AppendLine($"GasUsed: {GasUsed}");
			}

			return sb.ToString();
		}
	}

	[Serializable]
	public class Message
	{
		public List<Tag> Tags { get; set; } = new List<Tag>();
		public string Data { get; set; }
		public string Anchor { get; set; }
		public string Target { get; set; }

		public string GetTagValue(string tagName)
		{
			Tag tag = Tags.Find(t => t.Name == tagName);
			return tag != null ? tag.Value : null;
		}

		public Message(JSONNode messageNode)
		{
			if (messageNode.HasKey("tags") || messageNode.HasKey("Tags"))
			{
				JSONNode tagsNode = messageNode.HasKey("tags") ? messageNode["tags"] : messageNode["Tags"];
				foreach (var tagNode in tagsNode.AsArray)
				{
					Tags.Add(new Tag(tagNode));
				}
			}

			Data = messageNode.HasKey("data") ? messageNode["data"] : messageNode.HasKey("Data") ? messageNode["Data"] : null;
			Anchor = messageNode.HasKey("anchor") ? messageNode["anchor"] : messageNode.HasKey("Anchor") ? messageNode["Anchor"] : null;
			Target = messageNode.HasKey("target") ? messageNode["target"] : messageNode.HasKey("Target") ? messageNode["Target"] : null;
		}
	}

	[Serializable]
	public class MessageSU : Message
	{
		public string Id { get; set; }
		public Owner Owner { get; set; }
		public string Signature { get; set; }

		public MessageSU(JSONNode messageNode) : base(messageNode)
		{
			Id = messageNode.HasKey("id") ? messageNode["id"] : null;
			Owner = messageNode.HasKey("owner") ? new Owner(messageNode["owner"]) : null;
			Signature = messageNode.HasKey("signature") ? messageNode["signature"] : null;
		}
	}

	[Serializable]
	public class Owner
	{
		public string Address { get; set; }
		public string Key { get; set; }

		public Owner(JSONNode ownerNode)
		{
			Address = ownerNode["address"];
			Key = ownerNode["key"];
		}
	}


	[Serializable]
	public class Tag
	{
		public string Name { get; set; }
		public string Value { get; set; }

		public Tag(JSONNode tagNode)
		{
			Name = tagNode.HasKey("name") ? tagNode["name"] : null;
			Value = tagNode.HasKey("value") ? tagNode["value"] : null;
		}

		public Tag(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public JSONObject ToJson()
		{
			var json = new JSONObject();
			json["name"] = Name;
			json["value"] = Value;
			return json;
		}
	}

	[Serializable]
	public class Output
	{
		public string Data;
		public bool Print;
		public string Prompt;
	}
}