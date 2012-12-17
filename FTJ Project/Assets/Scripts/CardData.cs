public class CardData
{
	public int card_id = -1;
	public int card_back_id = -1;
	
	public CardData() { }
	public CardData(CardData other) {
		card_id = other.card_id;
		card_back_id = other.card_back_id;
	}
	public CardData(int card_id_, int card_back_id_) {
		card_id = card_id_;
		card_back_id = card_back_id_;
	}
}
