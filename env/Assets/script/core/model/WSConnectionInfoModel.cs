
using System;

public class WSConnectionInfoModel {

    private String url;
    private String connectionType;
    private String name;


    public WSConnectionInfoModel(String url, String connectionType, String name){
        this.url = url;
        this.connectionType = connectionType;
        this.name = name;
    }

    public void setUrl(String url){
        this.url = url;
    }

    public void setConnectionType(String connectionType){
        this.connectionType = connectionType;
    }

    public void setName(String name){
        this.name = name;
    }

    public String getUrl(){
        return url;
    }

    
    public String getConnectionType(){
        return connectionType;
    }

    public String getName(){
        return name;
    }

}