package artifact.lib.model.item;

public enum ShopItemEnum {
    // Fruits
    Apple("apple"),
    Banana("banana"),
    Orange("orange"),
    Mango("mango"),
    Pineapple("pineapple"),
    Strawberry("strawberry"),
    Grape("grape"),
    Pear("pear"),
    Peach("peach"),
    Watermelon("watermelon"),

    // Clothes
    Shirt("shirt"),
    Pants("pants"),
    Jacket("jacket"),
    Shoes("shoes"),
    Hat("hat"),
    Dress("dress"),
    Skirt("skirt"),
    Shorts("shorts"),
    Sweater("sweater"),

    // Coffee
    Espresso("espresso"),
    Americano("americano"),
    Cappuccino("cappuccino"),
    Latte("latte"),
    Mocha("mocha"),
    Macchiato("macchiato"),
    FlatWhite("flatWhite"),
    BlackCoffee("blackCoffee"),
    IcedCoffee("icedCoffee"),
    ColdBrew("coldBrew"),
    Affogato("affogato"),
    Cortado("cortado"),
    RedEye("redEye"),
    Vienna("vienna"),
    IrishCoffee("irishCoffee"),
    NoValue("novalue");

    // Fields to store the custom value and item type
    private final String displayName;

    // Constructor to assign the custom value and item type
    ShopItemEnum(String displayName) {
        this.displayName = displayName;
    }

    // Getter to retrieve the custom value
    public String getDisplayName() {
        return displayName;
    }

    @Override
    public String toString() {
        return displayName;
    }
}