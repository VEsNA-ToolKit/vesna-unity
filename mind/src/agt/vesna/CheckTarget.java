package vesna;

import jason.asSemantics.*;
import jason.asSyntax.*;

import java.io.*;
import java.nio.file.*;

public class CheckTarget extends DefaultInternalAction {

    private static final String TARGET_FILE =
            System.getProperty("user.home")
            + "\\AppData\\LocalLow\\DefaultCompany\\JaCaMoIntegration\\graph_snapshots\\target.json";

    private String lastSignature = null;

    @Override
    public Object execute(TransitionSystem ts, Unifier un, Term[] args) throws Exception {

        File f = new File(TARGET_FILE);
        if (!f.exists()) return false;

        String content;
        try {
            content = new String(Files.readAllBytes(f.toPath()), "UTF-8");
        } catch (IOException e) {
            return false;
        }

        String target = extractStringField(content, "target");
        if (target == null || target.isEmpty()) return false;

        String artifact = extractStringField(content, "artifact");
        if (artifact == null) artifact = "";

        String signature = target + "|" + artifact;
        if (signature.equals(lastSignature)) return false;
        lastSignature = signature;

        boolean ok = un.unifies(args[0], ASSyntax.createString(target));

        if (args.length >= 2) {
            ok = ok && un.unifies(args[1], ASSyntax.createString(artifact));
        }

        return ok;
    }

    private String extractStringField(String json, String key) {
        int keyIdx = json.indexOf("\"" + key + "\"");
        if (keyIdx < 0) return null;
        int colonIdx = json.indexOf(':', keyIdx);
        if (colonIdx < 0) return null;
        int firstQuote = json.indexOf('"', colonIdx + 1);
        if (firstQuote < 0) return null;
        int secondQuote = json.indexOf('"', firstQuote + 1);
        if (secondQuote < 0) return null;
        return json.substring(firstQuote + 1, secondQuote);
    }

    private Double extractNumberField(String json, String key) {
        int keyIdx = json.indexOf("\"" + key + "\"");
        if (keyIdx < 0) return null;
        int colonIdx = json.indexOf(':', keyIdx);
        if (colonIdx < 0) return null;
        int i = colonIdx + 1, n = json.length();
        while (i < n && Character.isWhitespace(json.charAt(i))) i++;
        int start = i;
        while (i < n && "+-0123456789.eE".indexOf(json.charAt(i)) >= 0) i++;
        if (i == start) return null;
        try {
            return Double.parseDouble(json.substring(start, i));
        } catch (NumberFormatException e) {
            return null;
        }
    }
}