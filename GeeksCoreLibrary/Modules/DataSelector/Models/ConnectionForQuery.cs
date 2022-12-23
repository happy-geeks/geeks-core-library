using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.DataSelector.Models;

public class ConnectionForQuery
{
    /// <summary>
    /// Gets or sets the part of the query needed to JOIN this connection.
    /// </summary>
    public string JoinQueryPart { get; set; }
    
    public IList<FieldForQuery> Fields { get; set; }
}