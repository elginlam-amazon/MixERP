/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using System.Runtime.InteropServices;

// Assembly-level attributes carried over from the legacy Properties/AssemblyInfo.cs.
// The SDK generates the identity/metadata attributes from MSBuild properties in the
// .csproj, but these two have no MSBuild property equivalent and must stay in source:
//  - CLSCompliant(true) drives CLS-compliance diagnostics over this assembly's public
//    surface (which exposes Npgsql-typed members through MixERP.Net.DbFactory).
//  - Guid preserves the assembly's typelib identity from the original AssemblyInfo.cs.
[assembly: CLSCompliant(true)]
[assembly: Guid("d7b4a31c-1003-4ff1-a1e4-a668b8de8a5e")]
