namespace ReverseSpectre.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_comment_to_documentfile : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LoanApplicationDocumentFiles", "Comment", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LoanApplicationDocumentFiles", "Comment");
        }
    }
}
