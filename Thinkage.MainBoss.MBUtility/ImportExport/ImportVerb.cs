using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MBUtility {
	internal class ImportVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(SchemaIdentification = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("SchemaIdentification"), KB.I("Identity of schema"), false));
				Optable.Add(InputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Input"), KB.I("File containing the import data"), true));
				Optable.Add(ErrorOutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ErrorOutput"), KB.I("File containing the errors encountered during import."), false));
			}
			public readonly StringValueOption SchemaIdentification;
			public readonly StringValueOption InputFile;
			public readonly StringValueOption ErrorOutputFile;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "Import";
				}
			}
			public override void RunVerb() {
				new ImportVerb(this).Run();
			}
		}
		private ImportVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;

		private void Run() {
			DataImportExportHelper.Setup();
			DataImportExport id = new DataImportExport(DataImportExportHelper.ValidateSchemaIdentification(Options.SchemaIdentification.Value));
			// See if we can successfully read the input data file and it passes schema validation
			id.LoadDataSetFromXmlFile(Options.InputFile.Value, new System.Xml.Schema.ValidationEventHandler(Xml_ValidationEventHandler));

			// Get a connection to the database that we are importing to
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			DataImportExportHelper.SetupDatabaseAccess(oName, connect);
			Thinkage.Libraries.DBAccess.DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			DBIDataSet errors;
			using (dsMB mbds = new dsMB(db)) {
				mbds.DisableUpdatePropagation();
				errors = id.SaveDataSetToDatabase(mbds);
			}
			DataImportExportHelper.CheckAndSaveErrors(Options.ErrorOutputFile.HasValue ? Options.ErrorOutputFile.Value : null, errors, DataImportExport.CheckErrorDataSet(errors));
		}

		/// <summary>
		/// Validate the import data against the schema provided as the first test
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Xml_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e) {
			System.Console.Write(Strings.Format(KB.K("Line {0}, Position {1}"), e.Exception.LineNumber, e.Exception.LinePosition));
			System.Console.WriteLine(Strings.IFormat(":{0}", e.Message));
		}
	}
}
