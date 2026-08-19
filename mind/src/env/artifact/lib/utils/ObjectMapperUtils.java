package artifact.lib.utils;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.apache.commons.text.StringEscapeUtils;

public class ObjectMapperUtils {

    private static ObjectMapper objectMapper = new ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

    public static <T, R> R convertObject(T sourceObject, Class<R> targetClass) {
       objectMapper.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
        return objectMapper.convertValue(sourceObject, targetClass);
    }

    public static <T, R> R convertObject(T sourceObject, TypeReference<R> targetClass) {
        return objectMapper.convertValue(sourceObject, targetClass);
    }


    // Method used to convert json string in every other object
    public static <T> T convertJsonStringToObject(String jsonString, TypeReference<T> typeReference) {
        try {
            // Used to unescape json string used to initialize artifact properties into .jcm file
            String unescapedJson = StringEscapeUtils.unescapeJava(jsonString);
            return objectMapper.readValue(unescapedJson, typeReference);
        } catch (Exception e) {
            throw new RuntimeException("Error converting JSON to object", e);
        }
    }

    public static <T> String convertIntoJsonString(T object){
        try{
            return objectMapper.writeValueAsString(object);
        }catch (Exception e){
            System.out.println("Exception occurred when convert object into json string. " + e);
        }
        return null;
    }
}
