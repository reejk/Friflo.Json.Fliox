// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using UserStore2.DB.Auth.Rights;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.DB.UserAuth {

public class UserPermission {
    string        id;
    List<string>  roles;
}

public class UserCredential {
    string  id;
    string  passHash;
    string  token;
}

public class Role {
    [Fri.Required]
    string       id;
    [Fri.Required]
    List<Right>  rights;
    string       description;
}

}

