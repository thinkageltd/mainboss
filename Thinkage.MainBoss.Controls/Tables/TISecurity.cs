using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	// The permissions Loading code in DBVersionHandler/MB3Application does not support groups at all.
	// VISIBLE_PERMISSIONS turns on the UI for showing general Permissions.
	// The program supports permissions properly, but the UI for editing them in a sane manner is lacking.
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Security.
	/// </summary>
	public class TISecurity : TIGeneralMB3 {
		public static DelayedCreateTbl UserPickerTblCreator;
		public static DelayedCreateTbl ManageDatabaseUserTblCreator;
		public static DelayedCreateTbl ManageDatabaseLoginTblCreator;

		public static DelayedCreateTbl RoleBrowserTblCreator;
		public static DelayedCreateTbl BuiltinRoleEditTblCreator;
		public static DelayedCreateTbl CustomRoleEditTblCreator;

		public static DelayedCreateTbl UserRoleBrowserTblCreator;
		private static DelayedCreateTbl UserRoleBuiltinEditTblCreator;
		private static DelayedCreateTbl UserRoleCustomEditTblCreator;
		#region Permission
		private static readonly DelayedCreateTbl PermissionForRoleTblCreator;
		private static readonly DelayedCreateTbl PermissionForCustomRoleTblCreator;
		private static Check CheckPermissionPattern(object patternID) {
			return new Check1<string>(delegate (string pattern) {
				Thinkage.Libraries.Permissions.IRightGrantor pm = Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager;
				GeneralException valid = pm.ValidatePermission(pattern);
				if (valid != null)
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(valid);
				return null;
			}).Operand1(patternID);
		}
		private static Tbl CreatePermissionEditor(bool isForCustom) {
			object patternCheckID = KB.I("patternCheckID");
			return new Tbl(dsMB.Schema.T.Permission, TId.Permission,
				new Tbl.IAttr[] {
						SecurityGroup,
						new BTbl(BTbl.ListColumn(isForCustom ? dsMB.Path.T.Permission.F.PrincipalID.F.CustomRoleID.F.Code : dsMB.Path.T.Permission.F.PrincipalID.F.RoleID.F.RoleName),
							BTbl.ListColumn(dsMB.Path.T.Permission.F.PermissionPathPattern)
						),
						isForCustom ? new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)) : new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Permission.F.PrincipalID, Fmt.SetPickFrom(RoleBrowserTblCreator), new NonDefaultCol(),
						isForCustom ? new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CustomRole.F.Code)) : new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Role.F.RoleName)), ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.Permission.F.PermissionPathPattern, DCol.Normal, new ECol(Fmt.SetId(patternCheckID)))
				),
				CheckPermissionPattern(patternCheckID)
			);
		}
		#endregion
		private TISecurity() {
		}
		static TISecurity() {
			PermissionForRoleTblCreator = new DelayedCreateTbl(() => CreatePermissionEditor(false));
			PermissionForCustomRoleTblCreator = new DelayedCreateTbl(() => CreatePermissionEditor(true));
			ManageDatabaseLoginTblCreator = new DelayedCreateTbl(() => ManageDatabaseLoginTbl);
			ManageDatabaseUserTblCreator = new DelayedCreateTbl(() => ManageDatabaseUserTbl);
		}

		#region ManageDatabaseUserTbl
		public static Tbl ManageDatabaseUserTbl = new Tbl(dsSecurityOnServer.Schema.T.SecurityOnServer, TId.SQLDatabaseUser,
			new Tbl.IAttr[] {
				SecurityGroup,
				new UseNamedTableSchemaPermissionTbl(dsMB.Schema.T.User),
				new DynamicPermissionTbl(delegate(ISession session, TableOperationRightsGroup.TableOperation operation) {
					if( operation == TableOperationRightsGroup.TableOperation.Create || operation == TableOperationRightsGroup.TableOperation.Delete || operation == TableOperationRightsGroup.TableOperation.Browse)
						return new SettableDisablerProperties(null, KB.K("You require SQL Database User Administration permissions for this operation"), session.CanManageUserCredentials());
					return null;
				}),
				new CustomSessionTbl(delegate(DBClient existingDBAccess, DBI_Database newSchema) { return new SecurityOnServerSession.Connection(existingDBAccess, forLogins:false); }),
				new BTbl(
					BTbl.ListColumn(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName),
					BTbl.ListColumn(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName),
					BTbl.ListColumn(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod),
					BTbl.LogicClass(typeof(ManageDatabaseCredentialBrowseLogic))
				),
				new ETbl(
					ETbl.LogicClass(typeof(DynamicEditLogic<ManageDatabaseCredentialEditTbl>)),
					ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Delete))
			},
			new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName, DCol.Normal, new ECol(ECol.ForceValue())),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName, DCol.Normal,
						new ECol(ECol.NormalAccess,
							Fmt.SetCreatorT<EditLogic>(CreateDatabaseLoginPicker)
						)
					),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.IsSysAdmin, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.Enabled, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.IsLoginManager, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.InMainBossRole, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.IsDBO, DCol.Normal)
				)
			)
		);
		#endregion
		#region CreateDatabaseLoginPicker
		private static IControl CreateDatabaseLoginPicker(EditLogic logicObject, TblLeafNode leafNode, Libraries.TypeInfo.TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer)
			=> logicObject.CommonUI.UIFactory.CreateTextEditWithPickButton((StringTypeInfo)controlTypeInfo, enabledDisabler, writeableDisabler, control =>
					new List<MenuItem>() {
						new CommandMenuItem(KB.K("Select server login"), image: logicObject.CommonUI.UIFactory.PickWithBrowserImage, command: new MultiCommandIfAllEnabled(
							new CallDelegateCommand(
								KB.K("Select from the logins avaiable on the server"),
								delegate() {
									Libraries.Presentation.MSWindows.SelectValueForm.NewSelectValueForm(logicObject.CommonUI.UIFactory, logicObject.DB, ManageDatabaseLoginTbl, new BrowserPathValue(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName),
									delegate (object value) {
										control.Value = value;
									}, allowNull: false).ShowModal(control.ContainingForm);
								}),
							writeableDisabler))
					}
			);
		#endregion
		#region CreateDatabaseCredentialsPicker
		// This picker is for a semicolon-separated list of SQL logins to place in the User record's AuthenticationCredential field
		private class SQLLoginListEditTextHandler : TypeEditTextHandler {
			public SQLLoginListEditTextHandler(TypeInfo ti) {
				UnderlyingHandler = ti.GetTypeEditTextHandler(Application.Instance.FormatCultureInfo);
			}
			private readonly TypeEditTextHandler UnderlyingHandler;

			public SizingInformation SizingInformation => UnderlyingHandler.SizingInformation;
			public Libraries.Drawing.StringAlignment PreferredAlignment => UnderlyingHandler.PreferredAlignment;
			public event Notification FormattingChanged { add { UnderlyingHandler.FormattingChanged += value; } remove { UnderlyingHandler.FormattingChanged -= value; } }
			public string Format(object val) => UnderlyingHandler.Format(val);
			public string FormatForEdit(object val) => UnderlyingHandler.FormatForEdit(val);
			public TypeInfo GetTypeInfo() => UnderlyingHandler.GetTypeInfo();
			// This Regex matches any non-empty sequence of whitespace and semicolons
			private static readonly System.Text.RegularExpressions.Regex MatchSeparatorSequence
				= new System.Text.RegularExpressions.Regex("[\\s;]+");
			public object ParseEditText(string str) {
				// Remove spaces around semicolons, multiple semicolons, and initial and final semicolons.
				return MatchSeparatorSequence.Replace((string)UnderlyingHandler.ParseEditText(str), ";").Trim(new[] { ';' });
			}
		}
		private static IControl CreateDatabaseCredentialsPicker(EditLogic logicObject, TblLeafNode leafNode, Libraries.TypeInfo.TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer)
			=> logicObject.CommonUI.UIFactory.CreateTextEditWithPickButton(new SQLLoginListEditTextHandler(controlTypeInfo), enabledDisabler, writeableDisabler, control =>
					new List<MenuItem>() {
						new CommandMenuItem(KB.K("Select database user"), image: logicObject.CommonUI.UIFactory.PickWithBrowserImage, command: new MultiCommandIfAllEnabled(
							new CallDelegateCommand(
								KB.K("Select from the users avaiable in the database"),
								delegate() {
									Libraries.Presentation.MSWindows.SelectValueForm.NewSelectValueForm(logicObject.CommonUI.UIFactory, logicObject.DB, ManageDatabaseUserTbl,
										new BrowserPathValue(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName),
										delegate (object value) {
											if (control.Value == null)
												control.Value = value;
											else if (value != null) {
												// Add the picked name to the control's value unless it is already there
												// Rather than using Split we could build a Regex to do the test (;|^)value(;|$) though we would have to
												// Regex-escape the value. Or we could do a Contains/StartsWith/EndsWith check.
												string[] names = ((string)control.Value).ToLower().Split(';');
												if (!names.Contains(((string)value).ToLower()))
													control.Value = Strings.IFormat("{0};{1}", control.Value, value);
											}
										},
										allowNull: false
									).ShowModal(control.ContainingForm);
								}),
							writeableDisabler))
					}
			);
		#endregion
		#region ManageDatabaseCredentialEditTbl (not really a Tbl, but an Edit Tbl customizer, for both logins and users)
		// This generates a Tbl on the fly for SecutiryOnServer records based on what sorts of authentication the particular server allows.
		// Also based on the "ForDatabaseLoginUsers" property of the session, which means that we are looking at server login
		// or database users (I can't tell which way is which).
		private class ManageDatabaseCredentialEditTbl : IDynamicCustomTbl {
			public ManageDatabaseCredentialEditTbl() { }
			static readonly object methodControlId = KB.I("methodControlId");
			static readonly object passwordControlId = KB.I("passwordControlId");
			static readonly object dbusernameControlId = KB.I("dbusernameControlId");
			static readonly object dbloginnameControlId = KB.I("dbloginnameControlId");

			public Tbl CustomTbl(DBClient db) {
				var securitySession = db.Session as SecurityOnServerSession;
				bool forLoginCredentials = securitySession.ForDatabaseLoginUsers;
				Libraries.Collections.Set<AuthenticationMethod> permittedAuthenticationMethods = securitySession.Server.PermittedAuthenticationMethods(db.Session.ConnectionInformation, forLoginCredentials);

				Dictionary<Key, object> authenticationMethodChoices = new Dictionary<Key, object>();
				foreach (var am in permittedAuthenticationMethods)
					authenticationMethodChoices.Add(AuthenticationCredentials.AuthenticationMethodProvider.GetLabel(am), am);
				EnumValueTextRepresentations AuthenticationSettingsPermitted = new EnumValueTextRepresentations(authenticationMethodChoices.Keys.ToArray(), authenticationMethodChoices.Keys.ToArray(), authenticationMethodChoices.Values.ToArray(), 1);
				object initialChoice = AuthenticationSettingsPermitted.Values[0];

				bool needLoginChoices = permittedAuthenticationMethods.Contains(AuthenticationMethod.WindowsAuthentication);
				bool needPassword = permittedAuthenticationMethods.Contains(AuthenticationMethod.SQLPassword) || permittedAuthenticationMethods.Contains(AuthenticationMethod.ActiveDirectoryPassword);

				List<TblLayoutNode> nodes = new List<TblLayoutNode>();
				if (forLoginCredentials) {
					nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName, new ECol(ECol.ForceValue())));
					if (needPassword)
						nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.Password, ECol.Normal, Fmt.SetId(passwordControlId), Fmt.SetUsage(DBI_Value.UsageType.Password)));
				}
				else {
					if (needLoginChoices) {
						nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName,
							new ECol(ECol.NormalAccess,
								ECol.ForceValue(),
								Fmt.SetId(dbloginnameControlId),
								Fmt.SetCreatorT<EditLogic>(CreateDatabaseLoginPicker))
							)
						);
						// If login choices exist, the username is forced to be the same as the login name since we only save one 'credential' for access to the database and we use that credential to match the MainBoss User to the connection
						nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName, ECol.AllReadonly, Fmt.SetId(dbusernameControlId)));
					}
					else
						nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName, new ECol(ECol.ForceValue())));
					if (needPassword)
						nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.Password, ECol.Normal, Fmt.SetId(passwordControlId), Fmt.SetUsage(DBI_Value.UsageType.Password)));
				}
				bool offerMethodChoice = forLoginCredentials || needPassword;
				if (offerMethodChoice)
					nodes.Add(TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod,
						new ECol(
							Fmt.SetEnumText(AuthenticationSettingsPermitted),
							Fmt.SetId(methodControlId)
						)));
				var tblInits = new List<TblActionNode> {
					Init.OnLoadNew(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod, new ConstantValue(initialChoice))
				};
				if (needLoginChoices && !forLoginCredentials) {
					pInitLists[0].Add(Init.Continuous(new PathTarget(dsSecurityOnServer.Path.T.SecurityOnServer.F.DBUserName), new Libraries.Presentation.ControlValue(dbloginnameControlId)));
				}
				if (needPassword) {
					pInitLists[0].Add(Init.Continuous(new ControlReadonlyTarget(passwordControlId, KB.K("A password is not required for this authentication method"))
							, CalculatedInitValue.New<EditorInitValue>(BoolTypeInfo.NonNullUniverse, delegate (object[] inputs) {
								// determine if authentication method needs the password field
								return (inputs[0] != null &&
										!((AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) == AuthenticationMethod.SQLPassword
									|| (AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) == AuthenticationMethod.ActiveDirectoryPassword)
									);
							}, new Libraries.Presentation.ControlValue(methodControlId)))
					);
					tblInits.Add(new Check2<string, int>(delegate (string password, int am) {
						if (((AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(am, typeof(int)) == AuthenticationMethod.SQLPassword
									|| (AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(am, typeof(int)) == AuthenticationMethod.ActiveDirectoryPassword)
									&& password == null)
							return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, ObjectTypeInfo.NonNullValueRequiredExceptionObject);
						return null;
					}).Operand1(passwordControlId)
					.Operand2(methodControlId));
				}

				return new Tbl(dsSecurityOnServer.Schema.T.SecurityOnServer, forLoginCredentials ? TId.SQLDatabaseLogin : TId.SQLDatabaseUser,
					new Tbl.IAttr[] {
							SecurityGroup,
							new DynamicPermissionTbl(delegate(ISession session, TableOperationRightsGroup.TableOperation operation) {
								if (forLoginCredentials)
									return new SettableDisablerProperties(null, KB.K("You require SQL Database Login Administration permissions for this operation"), session.CanManageUserLogins());
								else
									return new SettableDisablerProperties(null, KB.K("You require SQL Database User Administration permissions for this operation"), session.CanManageUserCredentials());
							}),
							new CustomSessionTbl(delegate(DBClient existingDBAccess, DBI_Database newSchema) { return new SecurityOnServerSession.Connection(existingDBAccess, forLoginCredentials); }),
							new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Delete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(nodes.ToArray())
					),
					tblInits.ToArray());
			}
			private readonly List<TblActionNode>[] pInitLists = new List<TblActionNode>[] { new List<TblActionNode>() };
			public List<TblActionNode>[] InitLists {
				get {
					return pInitLists;
				}
			}
		}
		#endregion
		#region ManageDatabaseCredentialBrowseLogic for logins and users
		private class ManageDatabaseCredentialBrowseLogic : BrowseLogic {
			public ManageDatabaseCredentialBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
				: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
			}
#if TODO
			SettableDisablerProperties GetDisabler() {
			var session = (MainBoss.SecurityOnServerSession)DB.Session;
				if (session.ForDatabaseLoginUsers)
					return new SettableDisablerProperties(null, KB.K("You require SQL Database Login Administration permissions for this operation"), session.CanManageUserLogins());
				else
					return new SettableDisablerProperties(null, KB.K("You require SQL Database User Administration permissions for this operation"), session.CanManageUserCredentials());
			}
			//			public override void CreateLocalNewCommands(bool includeCommandsMarkedExportable, EditLogic.SavedEventHandler savedHandler, params IDisablerProperties[] extraDisablers) {
			//				List<IDisablerProperties> myDisablers = new List<IDisablerProperties>(extraDisablers);
			//				myDisablers.Add(GetDisabler());
			//				base.CreateLocalNewCommands(includeCommandsMarkedExportable, savedHandler, myDisablers.ToArray());
			//			}
			// TODO: Need method to add the disabler to the DeleteCommand
#endif
		}
		#endregion
		#region ManageDatabaseLoginTbl
		public static Tbl ManageDatabaseLoginTbl = new Tbl(dsSecurityOnServer.Schema.T.SecurityOnServer, TId.SQLDatabaseLogin,
			new Tbl.IAttr[] {
				SecurityGroup,
				new UseNamedTableSchemaPermissionTbl(dsMB.Schema.T.User), // part of User management
				new DynamicPermissionTbl(delegate(ISession session, TableOperationRightsGroup.TableOperation operation) {
					return new SettableDisablerProperties(null, KB.K("You require SQL Database Login Administration permissions for this operation"), session.CanManageUserLogins());
				}),
				new CustomSessionTbl(delegate(DBClient existingDBAccess, DBI_Database newSchema) { return new SecurityOnServerSession.Connection(existingDBAccess, forLogins:true); }),
				new BTbl(
					BTbl.ListColumn(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName),
					BTbl.ListColumn(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod),
					BTbl.LogicClass(typeof(ManageDatabaseCredentialBrowseLogic))
				),
				new ETbl(
					ETbl.LogicClass(typeof(DynamicEditLogic<ManageDatabaseCredentialEditTbl>)),
					ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Delete))
			},
			new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.LoginName, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.CredentialAuthenticationMethod, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.Enabled, DCol.Normal),
					TblColumnNode.New(dsSecurityOnServer.Path.T.SecurityOnServer.F.IsLoginManager, DCol.Normal)
				)
			)
		);
		#endregion
		internal static void DefineTblEntries() {

			DelayedCreateTbl UserEditTblCreator = new DelayedCreateTbl(
					delegate () {
						return new Tbl(dsMB.Schema.T.User, TId.User,
							new Tbl.IAttr[] {
								SecurityGroup,
								new MinimumDBVersionTbl(new System.Version(1,1,1,16)),
								new ETbl()
							},
							new TblLayoutNodeArray(
								DetailsTabNode.New(
									TblColumnNode.New(dsMB.Path.T.User.F.ContactID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Contact.F.Code)), ECol.Normal),
									TblColumnNode.New(dsMB.Path.T.User.F.Desc, DCol.Normal, ECol.Normal),
									TblColumnNode.New(dsMB.Path.T.User.F.AuthenticationCredential, DCol.Normal,
										new ECol(ECol.NormalAccess,
											Fmt.SetCreatorT<EditLogic>(CreateDatabaseCredentialsPicker)
										)
									),
									TblColumnNode.New(dsMB.Path.T.User.F.Comment, DCol.Normal, ECol.Normal)
								),
								BrowsetteTabNode.New(TId.SecurityRole, TId.User,
									TblColumnNode.NewBrowsette(dsMB.Path.T.UserRole.F.UserID, DCol.Normal, ECol.Normal)
								)
							)
						);
					}
				);
			DefineEditTbl(dsMB.Schema.T.User, UserEditTblCreator);
			RegisterExistingForImportExport(TId.User, dsMB.Schema.T.User);
			DelayedCreateTbl UserBrowserTblCreator = new DelayedCreateTbl(
				delegate () {
					return new CompositeTbl(dsMB.Schema.T.User, TId.User,
						new Tbl.IAttr[] {
							SecurityGroup,
							new MinimumDBVersionTbl(new System.Version(1,1,1,16)),
							new BTbl(
								BTbl.ListColumn(dsMB.Path.T.User.F.AuthenticationCredential),
								BTbl.ListColumn(dsMB.Path.T.User.F.ContactID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.User.F.Desc),
								BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.UserReport))
							)
						},
						CompositeView.ChangeEditTbl(UserEditTblCreator,
							CompositeView.AdditionalVerb(KB.K("Evaluate Security As"),
								delegate (BrowseLogic browserLogic, int viewIndex) {
									Libraries.DataFlow.Source userIdSource = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.User.F.Id, -1);
									ICommand baseCommand = new CallDelegateCommand(KB.K("View what operations are allowed in MainBoss using the permissions for the selected User"),
										delegate () {
											((ApplicationWithSingleDatabaseConnection)Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>()).SetUserRecordIDForPermissionsAndLoadPermissions((System.Guid)userIdSource.GetValue(), applyAdminAccess: false);
										});
									ITblDrivenApplication app = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
									MultiCommandIfAllEnabled result = new MultiCommandIfAllEnabled(
										baseCommand,
										// The following permission is the most direct way a user can obtain additional privileges to their own account
										// so we use it to control the ability to impersonate some other existing more-privileged user.
										(IDisablerProperties)app.PermissionsManager.GetPermission(((TableOperationRightsGroup)Root.Rights.Table.FindDirectChild(dsMB.Schema.T.UserRole.MainTableName)).Create)
									);
									return result;
								}
							)
						)
					);
				}
			);
			DefineBrowseTbl(dsMB.Schema.T.User, UserBrowserTblCreator);
#region Role
			BuiltinRoleEditTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Role, TId.BuiltinSecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault, EdtMode.Edit, EdtMode.New, EdtMode.Delete, EdtMode.Clone))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.Role.F.RoleName, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Role.F.RoleDesc, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Role.F.RoleComment, DCol.Normal, ECol.AllReadonly)),
						BrowsetteTabNode.New(TId.Permission, TId.SecurityRole,
							TblColumnNode.NewBrowsette(PermissionForRoleTblCreator, dsMB.Path.T.Role.F.PrincipalID, dsMB.Path.T.Permission.F.PrincipalID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.User, TId.SecurityRole,
							TblColumnNode.NewBrowsette(dsMB.Path.T.Role.F.PrincipalID, dsMB.Path.T.UserRole.F.PrincipalID, DCol.Normal, ECol.Normal))
					)
				);
			});
			DefineEditTbl(dsMB.Schema.T.Role, BuiltinRoleEditTblCreator);
			CustomRoleEditTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.CustomRole, TId.CustomSecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.CustomRole.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.CustomRole.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.CustomRole.F.Comment, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Permission, TId.SecurityRole,
							TblColumnNode.NewBrowsette(PermissionForCustomRoleTblCreator, dsMB.Path.T.CustomRole.F.PrincipalID, dsMB.Path.T.Permission.F.PrincipalID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.User, TId.SecurityRole,
							TblColumnNode.NewBrowsette(dsMB.Path.T.CustomRole.F.PrincipalID, dsMB.Path.T.UserRole.F.PrincipalID, DCol.Normal, ECol.Normal))
					)
				);
			});
			DefineEditTbl(dsMB.Schema.T.CustomRole, CustomRoleEditTblCreator);
			RoleBrowserTblCreator = new DelayedCreateTbl(delegate () {
				object NameID = KB.I("NameID");
				object DescID = KB.I("DescID");
				return new CompositeTbl(dsMB.Schema.T.Principal, TId.SecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new MinimumDBVersionTbl(new System.Version(1,0,10,32)),
						new BTbl(
							BTbl.PerViewListColumn(CommonCodeColumnKey, NameID),
							BTbl.PerViewListColumn(CommonDescColumnKey, DescID),
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.Principal.F.RoleID).IsNotNull().Or(new SqlExpression(dsMB.Path.T.Principal.F.CustomRoleID).IsNotNull())),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.RoleReport))
						)
					},
					new CompositeView(dsMB.Path.T.Principal.F.RoleID, CompositeView.RecognizeByValidEditLinkage(),
						BTbl.PerViewColumnValue(NameID, dsMB.Path.T.Role.F.RoleName, BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<SimpleKey>(originalSource, 50))),
						BTbl.PerViewColumnValue(DescID, dsMB.Path.T.Role.F.RoleDesc, BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<SimpleKey>(originalSource, 100)))),
					new CompositeView(dsMB.Path.T.Principal.F.CustomRoleID, CompositeView.RecognizeByValidEditLinkage(),
						BTbl.PerViewColumnValue(NameID, dsMB.Path.T.CustomRole.F.Code),
						BTbl.PerViewColumnValue(DescID, dsMB.Path.T.CustomRole.F.Desc))
				);
			});
#endregion
#region UserRole
			UserRoleCustomEditTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.UserRole, TId.UserSecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.Edit, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID, new ECol(Fmt.SetPickFrom(RoleBrowserTblCreator))),
						TblGroupNode.New(dsMB.Path.T.UserRole.F.PrincipalID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.CustomRoleID.F.Code, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.CustomRoleID.F.Desc, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.CustomRoleID.F.Comment, DCol.Normal)
						),
						TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID, ECol.Normal),
						TblGroupNode.New(dsMB.Path.T.UserRole.F.UserID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.ContactID.F.Code, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.Desc, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.Comment, DCol.Normal, ECol.AllReadonly)
						)
					)
				);
			});
			UserRoleBuiltinEditTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.UserRole, TId.UserSecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.Edit, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID, new ECol(Fmt.SetPickFrom(RoleBrowserTblCreator))),
						TblGroupNode.New(dsMB.Path.T.UserRole.F.PrincipalID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID.F.RoleName, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID.F.RoleDesc, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID.F.RoleComment, DCol.Normal)
						),
						TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID, ECol.Normal),
						TblGroupNode.New(dsMB.Path.T.UserRole.F.UserID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.ContactID.F.Code, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.Desc, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UserRole.F.UserID.F.Comment, DCol.Normal, ECol.AllReadonly)
						)
					)
				);
			});

			UserRoleBrowserTblCreator = new DelayedCreateTbl(delegate () {
				object NameID = KB.I("NameID");
				SimpleKey NewUserRoleLabel = KB.K("Assign Role");
				return new CompositeTbl(dsMB.Schema.T.UserRole, TId.UserSecurityRole,
					new Tbl.IAttr[] {
						SecurityGroup,
						new MinimumDBVersionTbl(new System.Version(1,0,10,32)),
						new BTbl(
							BTbl.PerViewListColumn(KB.TOi(TId.SecurityRole), NameID),
							BTbl.ListColumn(dsMB.Path.T.UserRole.F.UserID.F.ContactID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.UserRole.F.UserID.F.AuthenticationCredential)
						)
					},
					CompositeView.ChangeEditTbl(UserRoleBuiltinEditTblCreator, CompositeView.JoinedNewCommand(NewUserRoleLabel), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID).IsNotNull()),
						BTbl.PerViewColumnValue(NameID, dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID.F.RoleName, BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<SimpleKey>(originalSource, 50)))),
					CompositeView.ChangeEditTbl(UserRoleCustomEditTblCreator, CompositeView.JoinedNewCommand(NewUserRoleLabel), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UserRole.F.PrincipalID.F.CustomRoleID).IsNotNull()),
						BTbl.PerViewColumnValue(NameID, dsMB.Path.T.UserRole.F.PrincipalID.F.CustomRoleID.F.Code))
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.UserRole, UserRoleBrowserTblCreator);
#endregion
		}
	}
}
