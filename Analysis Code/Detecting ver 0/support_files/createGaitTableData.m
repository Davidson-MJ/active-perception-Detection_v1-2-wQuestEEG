function Ppant_gaitData= createGaitTableData(dataIN, cfg)

%% function to clean up the handling and creation of a table, for easier analysis.
%( called from j2_binDatabycycle, and j3_binData_byLinkedCycles.

    subjID = cfg.subjID; % used in rejTrials_questv1
    nGait = cfg.nGait;

   [Trial, gaitIdx, targTime, targContrastIndx, targContrast, targDetected, targPosPcnt,...
       targRT, clickTime, clickPcnt, clickCorrect, clickRT, FApcnt]=deal(NaN);
   pkFoot = "Lft"'; % for example.
   
   %now create table, order columns;
   Ppant_gaitData=table(Trial, gaitIdx, pkFoot, targTime, targContrastIndx, targContrast, targDetected,...
       targPosPcnt, targRT, clickTime, clickPcnt, clickCorrect, clickRT, FApcnt);
  
   varnames=  Ppant_gaitData.Properties.VariableNames;  
   autorow = Ppant_gaitData(1,:); % used to prefill and suppress warnings later
%  

    %%
    gcounter=1;
    for itrial = 1:size(dataIN,2)
        if dataIN(itrial).isPrac || dataIN(itrial).isStationary
            continue
        end
         %% subj specific trial rejection
        skip=0;
        rejTrials_questv1; %toggles 'skip' based on bad trial ID               
        if skip==1
            continue
        end
       
        if nGait==1
        gd= dataIN(itrial).gaitData;
        else
        gd= dataIN(itrial).gaitData_doubGC;
        end
            
        for igait = 1:size(gd,2)
             %% for every event, store the relevant trial and gait index:
            % targ onsets (and correct responses first).

            %prefill table row to avoid auto warnings.
            Ppant_gaitData(gcounter, 1:length(varnames)) = autorow;
            % 
            %% any targets presented?

            ntargs = length(gd(igait).tOnset_inGait);
            %% any responses recorded, to targets?            
            if isfield(gd(igait),'response_resamp') && ~isempty(gd(igait).response_resamp)
                nresps = length(gd(igait).response_rawsamp);
            else
                nresps=0;
            end              
            %% any false alarms?
            % amount of resps recorded:
            if isfield(gd(igait),'FA_resamp') && ~isempty(gd(igait).FA_resamp)
                nFAs= length(gd(igait).FA_resamp);
            else
                nFAs=0;
            end
            
            
            
            %% plot what we (should) be storing
%             plotdebugGait;      

            
            %% Targets first:
                Ppant_gaitData.Trial(gcounter) = itrial; % trial
                Ppant_gaitData.gaitIdx(gcounter) = igait; % step in trial
                Ppant_gaitData.pkFoot(gcounter) =gd(igait).peak; % gait type
           
                usedResp=[]; % we will omit saving information twice, if already stored
                % with the relevant target.
            for itgOns = 1:ntargs
               
                % what happened this gait? If a double event, more care needs to
                % be taken:
                  %store info   
                  Ppant_gaitData.targTime(gcounter) = gd(igait).tOnset_inTrialTime(itgOns); % what happened this gait?
                  Ppant_gaitData.targDetected(gcounter) = gd(igait).tTargDetected(itgOns); % what happened this gait?
                  Ppant_gaitData.targPosPcnt(gcounter) = gd(igait).tOnset_inGaitResampled(itgOns); % which pcnt did targ Onset occur?
                  Ppant_gaitData.targRT(gcounter)= gd(igait).targRT_fromsmry(itgOns); % RT for this targ
                  Ppant_gaitData.targContrastIndx(gcounter) = gd(igait).tContrastIDX(itgOns);
                  Ppant_gaitData.targContrast(gcounter) = gd(igait).tContrast(itgOns);
                  
                  if itgOns>1 % if the second target in a gait, bring forward prev, gait info.
                       Ppant_gaitData(gcounter,1:3) =  Ppant_gaitData(gcounter-1,1:3); % trial
                  end
%                   
                  %if response recorded to this target, this gait.
                  if isfield(gd(igait),'response_resamp') && ~isempty(gd(igait).response_resamp)                     
                          % store the responses collected:                          
                         
                          for iresp=1:nresps
                              responseIDX = gd(igait).response_resamp(iresp); %
                              tOnsetTime =Ppant_gaitData.targTime(gcounter);
                              responsetrialTime =  gd(igait).response_rawtime(iresp); 
                              %if we haven't already stored this info, and
                              %it is (after) the target, add the click
                              %data.
                              if ~ismember(responseIDX, usedResp) && (responsetrialTime >= tOnsetTime);
                                  
                                  Ppant_gaitData.clickTime(gcounter) = gd(igait).response_rawtime(iresp); %click pos in gait (resampled).%
                                  Ppant_gaitData.clickPcnt(gcounter) = gd(igait).response_resamp(iresp); %click pos in gait (resampled).
                                  Ppant_gaitData.clickCorrect(gcounter) = gd(igait).response_corr(iresp); %click corr or no this  gait 
                                  Ppant_gaitData.clickRT(gcounter) = gd(igait).response_rawRT_fromsmry(iresp); %click RT
                                  usedResp= [usedResp,Ppant_gaitData.clickPcnt(gcounter)]; 
                                  
                                  if nresps>1
                                  gcounter=gcounter+1;
                                  %prefill row
                                  Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; % 
                                  end
                              end
                          end
%                   
                  end
              % may get some empty rows.    
              gcounter=gcounter+1;
              %prefill row to avoid injecting zeros!  
              Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; % 

            end % each target onset (and subsequent resp).
            
            % also store any extra responses (in case there are resps, without targ onsets) (e.g. an early response from the previous gait cycle).            
            
            for irspOns = 1:nresps                
                tryresp =gd(igait).response_resamp(irspOns);
                %only store the response if it hasn't already been included
                %in this analysis. (e.g. previous target onset phase above).
                if  ~ismember(tryresp,usedResp)
                %% now store all the resps that occured.
                Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; % 
                
                Ppant_gaitData.Trial(gcounter) = itrial; % trial
                Ppant_gaitData.gaitIdx(gcounter) = igait; % step in trial
                Ppant_gaitData.pkFoot(gcounter) =gd(igait).peak; % gait type
                % add the stored information for this (new) data:
           
                Ppant_gaitData.clickTime(gcounter) = gd(igait).response_rawtime(irspOns); 
                Ppant_gaitData.clickPcnt(gcounter) = gd(igait).response_resamp(irspOns); %click pos in gait (resampled).
                 Ppant_gaitData.clickCorrect(gcounter) = gd(igait).response_corr(irspOns); %
                Ppant_gaitData.clickRT(gcounter) = gd(igait).response_rawRT_fromsmry(irspOns); %click RT
               
                   gcounter=gcounter+1;
                   %prefill:
                   Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; %

                end
            end
            
            if nFAs~=0                                
                for iFA= 1:nFAs
                    gFA=gd(igait).FA_resamp(iFA);
                    
                    %% now store all the resps that occured.
                    Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; %
                    
                    Ppant_gaitData.Trial(gcounter) = itrial; % trial
                    Ppant_gaitData.gaitIdx(gcounter) = igait; % step in trial
                    Ppant_gaitData.pkFoot(gcounter) =gd(igait).peak; % gait type
                    
                    Ppant_gaitData.FApcnt(gcounter) = gFA;
                   
                    gcounter=gcounter+1;
                    %prefill.
                    Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; %

                end
            end
        
        %
%           
        
         
%also increment gait counter if no targs or resps:                
                if nresps==0 && ntargs==0 && nFAs==0
                    gcounter=gcounter+1;
                %prefill
                Ppant_gaitData(gcounter, 1:length(varnames)) = autorow; % 

                end
            end %igait
        
        
       end
    
    %%
    
 % also add a column for zscored versions of RTs (for later plots across
   % subjs).
    rtname = {'clickRT', 'targRT'};
   for iuseRT=1:2
   allrts = Ppant_gaitData.(rtname{iuseRT});
   % find index of all non nan.
   userow = find(~isnan(allrts));
   zrts = zscore(allrts(userow));
   %place in table as new columnL
   Ppant_gaitData.(['z_' rtname{iuseRT}]) = nan(size(Ppant_gaitData,1),1);
   Ppant_gaitData.(['z_' rtname{iuseRT}])(userow) = zrts;
   end  
    %%
     
    

end