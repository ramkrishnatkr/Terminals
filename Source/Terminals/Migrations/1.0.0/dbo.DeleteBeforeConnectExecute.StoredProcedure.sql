CREATE PROCEDURE [dbo].[DeleteBeforeConnectExecute]
	(
	@FavoriteId int
	)
AS
	delete from BeforeConnectExecute where FavoriteId = @FavoriteId
GO
