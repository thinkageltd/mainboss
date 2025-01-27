﻿using System;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public class BuiltinFunctionCreateUpgradeStep : UpgradeStep {
		// This step is used to Create a builtin function like dbo._IAdd
		public BuiltinFunctionCreateUpgradeStep([Invariant] string functionName) {
			FunctionName = functionName;
		}
		private readonly string FunctionName;
		public override void Reverse(Version startingVersion, DBI_Database schema) {
		}
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			session.ExecuteCommand(new DBSpecificCommandSpecification(Thinkage.Libraries.XAF.Database.Service.MSSql.Session.BuiltinDatabaseFunctions.Create(FunctionName)));
		}
	}
}