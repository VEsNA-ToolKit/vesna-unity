package artifact.lib.utils;

import jason.asSyntax.ASSyntax;
import jason.asSyntax.Literal;
import jason.asSyntax.parser.ParseException;

import java.lang.reflect.Field;
import java.util.ArrayList;
import java.util.List;


public class AgentUtils {

    public static Literal convertObjectToLiteral(Object obj) {
        // Retrieve object class name
        Class<?> clazz = obj.getClass();
        String className = clazz.getSimpleName()
                .toLowerCase()
                .split("model")[0];

        // Retrieve all fields value
        List<String> fieldValues = new ArrayList<>();

        // Get fields from the current class and all superclasses
        for (Class<?> c = clazz; c != null; c = c.getSuperclass()) {
            for (Field field : c.getDeclaredFields()) {
                field.setAccessible(true);
                try {
                    Object value = field.get(obj);
                    fieldValues.add(value.toString());
                } catch (IllegalAccessException e) {
                    e.printStackTrace();
                }
            }
        }
        String literalString = className + "(" + String.join(", ", fieldValues) + ")";
        return Literal.parseLiteral(literalString);
    }

    public static String cleanString(String msg){
        return msg.replace("\"", "");
    }

    public static Literal updateAgentMemory(Object[] seenElements, String elementToAdd) throws ParseException {
        List<String> filteredSeenElements = mapJacamoListIntoStringList(seenElements);
        // Check if element is already present in the memory
        if (!filteredSeenElements.contains(elementToAdd)) {
            filteredSeenElements.add(elementToAdd);
        }

        // Create a list string for the literal
        StringBuilder listBuilder = new StringBuilder("[");
        for (int i = 0; i < filteredSeenElements.size(); i++) {
            listBuilder.append(filteredSeenElements.get(i));
            if (i < filteredSeenElements.size() - 1) {
                listBuilder.append(", ");
            }
        }
        listBuilder.append("]");

        // Create the literal +updated_seen_elements([agents/artifacts])
        String literalString = "updated_seen_elements(" + listBuilder + ")";
        Literal literal = ASSyntax.parseLiteral(literalString);
        return literal;
    }

    // Method used to map the list of objects passed by agent into a list of strings
    public static List<String> mapJacamoListIntoStringList(Object[] seenElements) {
        List<String> filteredSeenElements = new ArrayList<>();
        // Convert all seenElements into a List<String>
        for (Object param : seenElements) {
            filteredSeenElements.add(param.toString());
        }
        return filteredSeenElements;
    }

}
