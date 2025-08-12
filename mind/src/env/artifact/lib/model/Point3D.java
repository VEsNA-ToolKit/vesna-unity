package artifact.lib.model;

public class Point3D {
    private double x;
    private double y;
    private double z;

    public Point3D(double x, double y, double z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public double getX() {
        return x;
    }

    public double getY() {
        return y;
    }

    public double getZ() {
        return z;
    }

    // Potentially useful to check the distance to the agent.
    public double getDistance(Point3D other) {
        return Math.sqrt(Math.pow(other.x - this.x, 2) +
                         Math.pow(other.y - this.y, 2) +
                         Math.pow(other.z - this.z, 2));
    }

    @Override
    public String toString() {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}
