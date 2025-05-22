using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CrossCutting.Contracts.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

public class TokenChangedEvent : BaseEvent
{
    public string Type { get; set; } // "GROUP_MEMBERSHIP"
    public string Operation { get; set; } // "CREATE"
    public string RealmId { get; set; } // "542ba4b4-7eec-44fc-be53-90146c817bed"
    public string ResourcePath { get; set; } // "users/e4db5890-0345-457a-8fff-99eaed61d5a7/groups/0380cdfa-f7ff-4689-8379-017cb3c68cf2"
    public long Timestamp { get; set; } // 1740525998195

    // Private field for JSON string
    private string _representation;

    // Public property for JSON string
    public string Representation
    {
        //get => _representation;
        set { _representation = value; }
    }

    //public GroupRepresentation? RepresentationSystemTextJsonConvert
    //{
    //    get
    //    {
    //        return System.Text.Json.JsonSerializer.Deserialize<GroupRepresentation>(_representation);
    //    }
    //}


    // Public property for deserialized object
    public GroupRepresentation? GroupRepresentation
    {
        get { return JsonConvert.DeserializeObject<GroupRepresentation>(_representation); }
    }
}

public class GroupRepresentation
{
    public string id { get; set; }
    public string name { get; set; }
    public string path { get; set; }
    public List<GroupRepresentation> subGroups { get; set; } = new();
    public Dictionary<string, List<string>> attributes { get; set; } = new();
    public List<string> realmRoles { get; set; } = new();
    public Dictionary<string, List<string>> clientRoles { get; set; } = new();
}
