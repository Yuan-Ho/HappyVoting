using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace HappyVoting8
{
	public class PreloadPaperResult : ActionResult
	{
		private readonly string voteId;

		public PreloadPaperResult(string vote_id)
		{
			this.voteId = vote_id;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			TextWriter writer = context.HttpContext.Response.Output;

			PaperRoll roll = PaperPond.Get(this.voteId);

			roll.Write(writer);
		}
	}
	public class PreloadTotalsResult : ActionResult
	{
		private readonly string voteId;

		public PreloadTotalsResult(string vote_id)
		{
			this.voteId = vote_id;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			TextWriter writer = context.HttpContext.Response.Output;

			IEnumerable<TallyInfo> totals = TotalStore.GetTotals(this.voteId);

			string output = JsonConvert.SerializeObject(totals);

			writer.Write(output);
		}
	}
	public class PreloadTagScrollResult : ActionResult
	{
		private readonly string tag;

		public PreloadTagScrollResult(string tag)
		{
			this.tag = tag;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			TextWriter writer = context.HttpContext.Response.Output;

			StringBuilder builder = new StringBuilder();
			int page_id = TagScrollStore.GetLastPageId(this.tag);

			if (page_id != -1)
			{
				if (page_id > 0)
					takePage(builder, page_id - 1);
				takePage(builder, page_id);
			}
			writer.Write(builder.ToString());
		}
		private void takePage(StringBuilder builder, int page_id)
		{
			IEnumerable<TaggedVoteInfo> scroll = TagScrollStore.GetTagScrollPage(this.tag, page_id);

			foreach (TaggedVoteInfo vote in scroll)
			{
				string hp = vote.ToHtmlPresentation();
				builder.AppendLine(hp);
			}
		}
	}
}