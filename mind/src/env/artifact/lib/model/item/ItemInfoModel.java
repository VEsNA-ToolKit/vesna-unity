package artifact.lib.model.item;

import lombok.*;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ItemInfoModel {

    private ShopItemEnum itemName;
    private double price;
    private int quantity;

}
