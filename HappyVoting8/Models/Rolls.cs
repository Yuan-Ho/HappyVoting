using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace HappyVoting8
{
	public class PaperRoll
	{
		private StringBuilder builder = new StringBuilder();
		public readonly string VoteId;
		private ConcurrentDictionary<string, string> settingsDict = new ConcurrentDictionary<string, string>();

		public PaperRoll(string vote_id)
		{
			this.VoteId = vote_id;

			PaperStore.GetAllActions(vote_id, entity =>
											{
												string hp = PaperStore.ActionToHtmlPresentation(entity);
												this.builder.AppendLine(hp);

												fishSetting(entity);
											});
		}
		public string AddMat(DynamicTableEntity entity)
		{
			string hp = PaperStore.ActionToHtmlPresentation(entity);

			lock (this.builder)
				this.builder.AppendLine(hp);

			fishSetting(entity);

			return hp;
		}
		private void fishSetting(DynamicTableEntity entity)
		{
			if (entity.GetString(Consts.TYPE_PROP_NAME, null) == "setg")
			{
				string mat = entity[Consts.MAT_PROP_NAME].StringValue;
				string value = entity[Consts.VALUE_PROP_NAME].StringValue;

				settingsDict[mat] = value;
			}
		}
		public string GetSetting(string mat)
		{
			string value;
			if (settingsDict.TryGetValue(mat, out value))
				return value;
			return null;
		}
		public void Write(TextWriter writer)
		{
			string text;

			lock (this.builder)
				text = this.builder.ToString();

			writer.Write(text);
		}
	}
}